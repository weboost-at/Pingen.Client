using System.Net;
using FluentAssertions;
using Pingen.Client.Batches;
using Pingen.Client.Batches.ValueTypes;
using Pingen.Client.Common;
using Pingen.Client.Deliveries.ValueTypes;
using Pingen.Client.Tests.Batches.Support;

namespace Pingen.Client.Tests.Batches;

public class BatchServiceTests : BatchTestBase
{
    [Fact]
    public async Task When_a_page_is_requested_ListAsync_gets_the_batches_path_with_the_query_and_maps_the_page()
    {
        // Arrange
        Host.Api.EnqueueOk(BatchPage(number: 1, lastPage: 2));

        var options = new PingenListOptions
        {
            PageNumber = 1,
            PageLimit = 1,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "processing"),
            Fields = new Dictionary<string, string> { ["batches"] = "name,status" }
        };

        // Act
        var page = await Batches.ListAsync(Organisation.Id, options, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, BatchesPath);
        Host.Api.Request.Assert.Query(
            """?page[number]=1&page[limit]=1&sort=-created_at&filter={"status":"processing"}&fields[batches]=name,status""");

        page.Should().ContainSingle();
        page[0].Meta.Should().BeNull();
        page[0].Attributes.Icon.Should().Be("wave-hand");
        page[0].Attributes.ChannelType.Should().Be("post");
        page[0].Attributes.SubmittedAt.Should().BeNull();
        page[0].Relationships!.Events!.Count.Should().Be(3);
        page.Meta!.Total.Should().Be(2);
        page.Links!.Next.Should().Be($"https://api.pingen.com{BatchesPath}?page[number]=2");
    }

    [Fact]
    public async Task When_two_pages_are_enumerated_ListAutoPagingAsync_yields_both_and_stops_at_the_last()
    {
        // Arrange
        Host.Api.EnqueueOk(BatchPage(number: 1, lastPage: 2)).EnqueueOk(BatchPage(number: 2, lastPage: 2));

        // Act
        var batches = await Batches
            .ListAutoPagingAsync(Organisation.Id, new() { PageLimit = 1 }, Ct)
            .ToListAsync(Ct);

        // Assert
        batches.Select(batch => batch.Attributes.LetterCount).Should().Equal(1, 2);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[0].Assert.Query("?page[limit]=1");
        Host.Api.Requests[1].Assert.Query("?page[number]=2&page[limit]=1");
    }

    [Fact]
    public async Task When_the_archive_is_uploaded_already_CreateAsync_posts_the_grouping_options_and_preset()
    {
        // Arrange
        Host.Api.EnqueueJson(HttpStatusCode.Created, BatchDocument());

        var options = new BatchCreateOptions
        {
            Name = Batch.Name,
            Icon = BatchIcon.WaveHand,
            FileOriginalName = Batch.FileOriginalName,
            FileUrl = File.Url.ToString(),
            FileUrlSignature = File.Signature,
            GroupingType = BatchGroupingType.Merge,
            SplitType = BatchSplitType.QrInvoice,
            ChannelType = BatchChannelType.Post,
            AddressPosition = AddressPosition.Right,
            SplitSize = 2,
            SplitSeparator = Batch.SplitSeparator,
            SplitPosition = BatchSplitPosition.FirstPage,
            PresetId = Batch.PresetId
        };

        // Act
        var batch = await Batches.CreateAsync(Organisation.Id, options, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        batch.Id.Should().Be(Batch.Id);

        Host.Api.Request.Assert.Sent(HttpMethod.Post, BatchesPath);
        Host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);

        Host.Api.Request.Data.Assert.Lacks("id");
        Host.Api.Request.Data.Assert.Has("type", "batches");
        Host.Api.Request.Attributes.Assert.Has("icon", "wave-hand");
        Host.Api.Request.Attributes.Assert.Has("channel_type", "post");
        Host.Api.Request.Attributes.Assert.Has("grouping_type", "merge");
        Host.Api.Request.Attributes.Assert.Has("name", Batch.Name.Default);
        Host.Api.Request.Attributes.Assert.Has("address_position", "right");
        Host.Api.Request.Attributes.Assert.Has("grouping_options_split_type", "qr_invoice");
        Host.Api.Request.Attributes.Assert.Has("grouping_options_split_position", "first_page");
        Host.Api.Request.Attributes.Assert.Has("grouping_options_split_separator", Batch.SplitSeparator);
        Host.Api.Request.Attributes.GetProperty("grouping_options_split_size").GetInt32().Should().Be(2);

        var preset = Host.Api.Request.Data.GetProperty("relationships").GetProperty("preset").GetProperty("data");
        preset.Assert.Has("id", Batch.PresetId.ToString());
    }

    [Fact]
    public async Task When_a_stream_is_given_CreateAsync_uploads_it_and_posts_the_presigned_url()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument()).EnqueueJson(HttpStatusCode.Created, BatchDocument());
        Host.Files.EnqueueEmpty(HttpStatusCode.OK);

        using var content = new MemoryStream(Batch.Archive);

        // Act
        var batch = await Batches.CreateAsync(Organisation.Id, content, BatchCreateOptions(), cancellationToken: Ct);

        // Assert
        batch.Id.Should().Be(Batch.Id);

        Host.Api.Requests[0].Path.Should().Be("/file-upload");

        Host.Files.Request.Body.Should().Equal(Batch.Archive);
        Host.Files.Request.Method.Should().Be(HttpMethod.Put);
        Host.Files.Request.Assert.Unauthenticated();

        Host.Api.Requests[1].Path.Should().Be(BatchesPath);
        Host.Api.Requests[1].Attributes.Assert.Has("file_url", File.Url.ToString());
        Host.Api.Requests[1].Attributes.Assert.Has("file_url_signature", File.Signature);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task When_the_options_carry_an_upload_target_CreateAsync_refuses_the_stream(bool url, bool signature)
    {
        // Arrange
        using var content = new MemoryStream(Batch.Archive);

        var options = BatchCreateOptions() with
        {
            FileUrl = url ? File.Url.ToString() : null,
            FileUrlSignature = signature ? File.Signature : null
        };

        // Act
        var act = () => Batches.CreateAsync(Organisation.Id, content, options, cancellationToken: Ct);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("options")
            .WithMessage("This overload uploads the file itself and fills FileUrl and FileUrlSignature - leave both unset.*");

        Host.Api.Requests.Should().BeEmpty();
        Host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_single_batch_is_requested_GetAsync_gets_its_path_and_maps_the_abilities()
    {
        // Arrange
        Host.Api.EnqueueOk(BatchDocument());

        // Act
        var batch = await Batches.GetAsync(Organisation.Id, Batch.Id, Ct);

        // Assert
        Host.Api.Request.Query.Should().BeEmpty();
        Host.Api.Request.Assert.Sent(HttpMethod.Get, BatchPath);
        Host.Api.Request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");

        batch.Attributes.PriceValue.Should().Be(1.25m);
        batch.Meta!.Abilities!["cancel"].Should().Be("ok");
    }

    [Fact]
    public async Task When_a_batch_is_renamed_EditAsync_patches_its_path_with_the_id_repeated_in_the_body()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        var options = new BatchEditOptions
        {
            Name = Batch.Name.Updated,
            Icon = BatchIcon.PercentTag
        };

        // Act
        await Batches.EditAsync(Organisation.Id, Batch.Id, options, cancellationToken: Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Patch, BatchPath);
        Host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);

        Host.Api.Request.Data.Assert.Has("type", "batches");
        Host.Api.Request.Data.Assert.Has("id", Batch.Id.ToString());
        Host.Api.Request.Attributes.Assert.Has("icon", "percent-tag");
        Host.Api.Request.Attributes.Assert.Has("name", Batch.Name.Updated);
    }

    [Fact]
    public async Task When_a_batch_is_deleted_DeleteAsync_sends_the_required_body_along_with_the_delete()
    {
        // Arrange
        Host.Api.EnqueueEmpty();

        // Act
        await Batches.DeleteAsync(Organisation.Id, Batch.Id, new() { WithLetters = true, WithDeliverables = false }, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Delete, BatchPath);
        Host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);

        Host.Api.Request.Data.Assert.Has("type", "batches");
        Host.Api.Request.Data.Assert.Has("id", Batch.Id.ToString());
        Host.Api.Request.Attributes.GetProperty("with_letters").GetBoolean().Should().BeTrue();
        Host.Api.Request.Attributes.GetProperty("with_deliverables").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task When_only_the_batch_is_deleted_DeleteAsync_omits_the_deliverables_flag()
    {
        // Arrange
        Host.Api.EnqueueEmpty();

        // Act
        await Batches.DeleteAsync(Organisation.Id, Batch.Id, new() { WithLetters = false }, Ct);

        // Assert
        Host.Api.Request.Attributes.Assert.Lacks("with_deliverables");
        Host.Api.Request.Attributes.GetProperty("with_letters").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task When_a_batch_is_cancelled_CancelAsync_patches_the_cancel_path_without_a_body()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await Batches.CancelAsync(Organisation.Id, Batch.Id, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{BatchPath}/cancel");
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);
    }

    [Fact]
    public async Task When_a_batch_is_sent_by_post_SendAsync_patches_the_send_path_with_the_print_options()
    {
        // Arrange
        Host.Api.EnqueueOk(BatchDocument());

        var options = BatchSendOptions.Post(DeliveryProduct.Fast, PrintMode.Simplex, PrintSpectrum.Color);

        // Act
        var batch = await Batches.SendAsync(Organisation.Id, Batch.Id, options, cancellationToken: Ct);

        // Assert
        batch.Attributes.Status.Should().Be("processing");

        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{BatchPath}/send");
        Host.Api.Request.Data.Assert.Has("id", Batch.Id.ToString());
        Host.Api.Request.Data.Assert.Has("type", "batches_channel_post_send");
        Host.Api.Request.Attributes.Assert.Has("print_mode", "simplex");
        Host.Api.Request.Attributes.Assert.Has("print_spectrum", "color");
        Host.Api.Request.Attributes.Assert.Has("delivery_product", "fast");
    }

    [Theory]
    [InlineData("batches_channel_email_send", "electronic_email")]
    [InlineData("batches_channel_ebill_send", "electronic_ebill")]
    public async Task When_a_batch_is_sent_electronically_SendAsync_omits_print_options(string type, string product)
    {
        // Arrange
        Host.Api.EnqueueOk(BatchDocument());

        var options = type is "batches_channel_email_send" ? BatchSendOptions.Email() : BatchSendOptions.Ebill();

        // Act
        var batch = await Batches.SendAsync(Organisation.Id, Batch.Id, options, cancellationToken: Ct);

        // Assert
        batch.Attributes.Status.Should().Be("processing");

        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{BatchPath}/send");
        Host.Api.Request.Data.Assert.Has("type", type);
        Host.Api.Request.Data.Assert.Has("id", Batch.Id.ToString());
        Host.Api.Request.Attributes.Assert.Lacks("print_mode");
        Host.Api.Request.Attributes.Assert.Lacks("print_spectrum");
        Host.Api.Request.Attributes.Assert.Has("delivery_product", product);
    }

    [Fact]
    public async Task When_the_events_of_a_batch_are_listed_ListEventsAsync_sends_the_language()
    {
        // Arrange
        Host.Api.EnqueueOk(EventsDocument());

        // Act
        var events = await Batches.ListEventsAsync(Organisation.Id, Batch.Id, new() { Language = "de-DE", Sort = "-emitted_at" }, Ct);

        // Assert
        Host.Api.Request.Path.Should().Be($"{BatchPath}/events");
        Host.Api.Request.Assert.Query("?sort=-emitted_at&language=de-DE");

        events.Should().ContainSingle();
        events[0].Type.Should().Be("batches_events");
        events[0].Attributes.Code.Should().Be("processing");
        events[0].Attributes.Data.Should().Equal("split");
        events[0].Relationships!.Batch!.Data!.Type.Should().Be("batches");
        events[0].Attributes.EmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
    }

    [Fact]
    public async Task When_the_statistics_are_requested_GetStatisticsAsync_maps_the_distributions()
    {
        // Arrange
        Host.Api.EnqueueOk(StatisticsDocument());

        // Act
        var statistics = await Batches.GetStatisticsAsync(Organisation.Id, Batch.Id, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, $"{BatchPath}/statistics");

        statistics.Type.Should().Be("batch_details_statistics");
        statistics.Attributes.LetterValidating.Should().Be(2);
        statistics.Attributes.LetterGroups.Select(group => group.Name).Should().Equal("valid", "not_available");

        statistics.Attributes.LetterCountries.Should().ContainSingle();
        statistics.Attributes.LetterCountries[0].Country.Should().Be("CH");

        statistics.Attributes.LetterRegions.Should().ContainSingle();
        statistics.Attributes.LetterRegions[0].Count.Should().Be(3);
    }

    [Fact]
    public async Task When_the_api_rejects_the_batch_CreateAsync_throws_with_the_errors_and_the_request_id()
    {
        // Arrange
        Host.Api.Enqueue(Rejection(HttpStatusCode.UnprocessableEntity));

        // Act
        var act = () => Batches.CreateAsync(Organisation.Id, BatchCreateOptions(), cancellationToken: Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.UnprocessableEntity);
        exception.Message.Should().Contain("422");
        exception.RequestId.Should().Be(Request.Id);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Source!.Pointer.Should().Be(Error.Pointer);
    }
}
