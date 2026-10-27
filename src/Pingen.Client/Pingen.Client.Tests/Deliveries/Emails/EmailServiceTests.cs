using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Deliveries.Emails.Support;

namespace Pingen.Client.Tests.Deliveries.Emails;

public class EmailServiceTests : EmailTestBase
{
    [Fact]
    public async Task When_options_shape_the_page_ListAsync_gets_the_delivery_list_with_the_query_and_maps_the_page()
    {
        // Arrange
        Host.Api.EnqueueOk(EmailPage(number: 2, lastPage: 2));

        var options = new PingenListOptions
        {
            PageNumber = 2,
            PageLimit = 50,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "sent"),
            Search = Email.Search,
            Include = "organisation",
            Fields = new Dictionary<string, string> { ["emails"] = "status,created_at" }
        };

        // Act
        var page = await Emails.ListAsync(Organisation.Id, options, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, EmailsPath);
        Host.Api.Request.Assert.Query(
            $$"""?page[number]=2&page[limit]=50&sort=-created_at&filter={"status":"sent"}&q={{Email.Search}}&include=organisation&fields[emails]=status,created_at""");

        page.Should().ContainSingle();
        page[0].Meta.Should().BeNull();
        page[0].Id.Should().Be(Email.Id);
        page[0].Type.Should().Be("emails");
        page[0].Attributes.FilePages.Should().Be(2);
        page[0].Attributes.Status.Should().Be("sent");
        page[0].Attributes.PriceValue.Should().Be(1.25m);
        page[0].Attributes.FileOriginalName.Should().Be(Email.FileOriginalName);
        page[0].Attributes.RecipientIdentifier.Should().Be(Email.MetaData.RecipientEmail);
        page[0].Attributes.SubmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        page[0].Attributes.UpdatedAt.Should().Be(new DateTimeOffset(2020, 11, 20, 10, 0, 0, TimeSpan.FromHours(1)));

        page[0].Relationships!.Batch!.Data.Should().BeNull();
        page[0].Relationships!.Events!.Count.Should().Be(3);
        page[0].Relationships!.Organisation!.Data!.Id.Should().Be(Organisation.Id.ToString());

        page.Meta!.Total.Should().Be(2);
        page.Links!.Self.Should().Be($"https://api.pingen.com{EmailsPath}?page[number]=2");
    }

    [Fact]
    public async Task When_the_collection_spans_pages_ListAutoPagingAsync_yields_every_item_and_asks_for_the_next_page()
    {
        // Arrange
        Host.Api.EnqueueOk(EmailPage(number: 1, lastPage: 2)).EnqueueOk(EmailPage(number: 2, lastPage: 2));

        // Act
        var emails = await Emails
            .ListAutoPagingAsync(Organisation.Id, new() { PageLimit = 1 }, Ct)
            .ToListAsync(Ct);

        // Assert
        emails.Select(email => email.Attributes.FilePages).Should().Equal(1, 2);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[0].Assert.Query("?page[limit]=1");
        Host.Api.Requests[1].Assert.Query("?page[number]=2&page[limit]=1");
    }

    [Fact]
    public async Task When_a_file_is_already_uploaded_CreateAsync_posts_the_envelope_without_an_id_and_with_the_preset()
    {
        // Arrange
        Host.Api.EnqueueJson(HttpStatusCode.Created, EmailDocument());

        var options = EmailCreateOptions() with
        {
            FileUrl = File.Url.ToString(),
            FileUrlSignature = File.Signature,
            PresetId = Email.PresetId
        };

        // Act
        var email = await Emails.CreateAsync(Organisation.Id, options, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        email.Id.Should().Be(Email.Id);
        email.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("cancel", "ok"));

        Host.Api.Request.Assert.Sent(HttpMethod.Post, EmailsPath);
        Host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);

        Host.Api.Request.Data.Assert.Lacks("id");
        Host.Api.Request.Data.Assert.Has("type", "emails");
        Host.Api.Request.Attributes.Assert.Lacks("PresetId");
        Host.Api.Request.Attributes.Assert.Has("file_original_name", Email.FileOriginalName);
        Host.Api.Request.Attributes.GetProperty("auto_send").GetBoolean().Should().BeTrue();

        var preset = Host.Api.Request.Data.GetProperty("relationships").GetProperty("preset").GetProperty("data");
        preset.Assert.Has("type", "presets");
        preset.Assert.Has("id", Email.PresetId.ToString());

        var metaData = Host.Api.Request.Attributes.GetProperty("meta_data");
        metaData.Assert.Has("subject", Email.MetaData.Subject);
        metaData.Assert.Has("content", Email.MetaData.Content);
        metaData.Assert.Has("reply_name", Email.MetaData.ReplyName);
        metaData.Assert.Has("reply_email", Email.MetaData.ReplyEmail);
        metaData.Assert.Has("sender_name", Email.MetaData.SenderName);
        metaData.Assert.Has("recipient_name", Email.MetaData.RecipientName);
        metaData.Assert.Has("recipient_email", Email.MetaData.RecipientEmail);
    }

    [Fact]
    public async Task When_no_url_is_known_yet_CreateAsync_uploads_the_stream_and_posts_its_url_and_signature()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument()).EnqueueJson(HttpStatusCode.Created, EmailDocument());
        Host.Files.EnqueueEmpty(HttpStatusCode.OK);

        using var content = new MemoryStream(File.Pdf);

        // Act
        await Emails.CreateAsync(Organisation.Id, content, EmailCreateOptions(), cancellationToken: Ct);

        // Assert
        Host.Api.Requests[0].Path.Should().Be("/file-upload");

        Host.Files.Request.Body.Should().Equal(File.Pdf);
        Host.Files.Request.Url.Should().Be(File.Url);
        Host.Files.Request.Method.Should().Be(HttpMethod.Put);
        Host.Files.Request.Assert.Unauthenticated();

        Host.Api.Requests[1].Assert.Sent(HttpMethod.Post, EmailsPath);
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
        using var content = new MemoryStream(File.Pdf);

        var options = EmailCreateOptions() with
        {
            FileUrl = url ? File.Url.ToString() : null,
            FileUrlSignature = signature ? File.Signature : null
        };

        // Act
        var act = () => Emails.CreateAsync(Organisation.Id, content, options, cancellationToken: Ct);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("options")
            .WithMessage("*fills FileUrl and FileUrlSignature from the upload it performs - leave both unset*");

        Host.Api.Requests.Should().BeEmpty();
        Host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_one_email_is_addressed_GetAsync_gets_it_and_maps_the_abilities()
    {
        // Arrange
        Host.Api.EnqueueOk(EmailDocument());

        // Act
        var email = await Emails.GetAsync(Organisation.Id, Email.Id, Ct);

        // Assert
        Host.Api.Request.Query.Should().BeEmpty();
        Host.Api.Request.Assert.Sent(HttpMethod.Get, EmailPath);

        email.Meta!.Abilities.Should().BeEquivalentTo(new Dictionary<string, string> { ["cancel"] = "ok", ["delete"] = "state" });
    }

    [Fact]
    public async Task When_an_email_is_dropped_DeleteAsync_deletes_it_without_a_body()
    {
        // Arrange
        Host.Api.EnqueueEmpty();

        // Act
        await Emails.DeleteAsync(Organisation.Id, Email.Id, Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Delete, EmailPath);
    }

    [Fact]
    public async Task When_an_email_is_still_cancellable_CancelAsync_patches_cancel_without_a_body()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await Emails.CancelAsync(Organisation.Id, Email.Id, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{EmailPath}/cancel");
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);
    }

    [Fact]
    public async Task When_the_file_endpoint_redirects_DownloadFileAsync_follows_the_location_without_authentication()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);
        Host.Files.EnqueueBytes(File.Pdf);

        using var downloaded = new MemoryStream();

        // Act
        await using var file = await Emails.DownloadFileAsync(Organisation.Id, Email.Id, Ct);
        await file.CopyToAsync(downloaded, Ct);

        // Assert
        downloaded.ToArray().Should().Equal(File.Pdf);

        Host.Api.Request.Path.Should().Be($"{EmailPath}/file");

        Host.Files.Request.Url.Should().Be(File.Location);
        Host.Files.Request.Assert.Unauthenticated();
    }

    [Fact]
    public async Task When_the_events_of_an_email_are_listed_ListEventsAsync_gets_them_localized_and_maps_the_parent()
    {
        // Arrange
        Host.Api.EnqueueOk(EventsDocument());

        // Act
        var events = await Emails.ListEventsAsync(Organisation.Id, Email.Id, new() { Language = "de-DE" }, Ct);

        // Assert
        Host.Api.Request.Assert.Query("?language=de-DE");
        Host.Api.Request.Path.Should().Be($"{EmailPath}/events");

        events.Should().ContainSingle();
        events[0].Type.Should().Be("deliverables_events");
        events[0].Attributes.Name.Should().Be("Zugestellt");
        events[0].Relationships!.Parent!.Data!.Type.Should().Be("emails");
    }

    [Fact]
    public async Task When_an_event_carries_an_image_GetEventImageLocationAsync_returns_the_location_unfollowed()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);

        // Act
        var location = await Emails.GetEventImageLocationAsync(Organisation.Id, Email.Id, Email.EventId, Ct);

        // Assert
        location.Should().Be(File.Location);

        Host.Files.Requests.Should().BeEmpty();
        Host.Api.Request.Path.Should().Be($"{EmailPath}/events/{Email.EventId}/image");
    }

    [Fact]
    public async Task When_an_event_image_is_downloaded_DownloadEventImageAsync_streams_the_redirect_target()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);
        Host.Files.EnqueueBytes(File.Pdf);

        using var downloaded = new MemoryStream();

        // Act
        await using var image = await Emails.DownloadEventImageAsync(Organisation.Id, Email.Id, Email.EventId, Ct);
        await image.CopyToAsync(downloaded, Ct);

        // Assert
        downloaded.ToArray().Should().Equal(File.Pdf);

        Host.Files.Request.Assert.Unauthenticated();
    }

    [Fact]
    public async Task When_the_create_call_is_rejected_CreateAsync_throws_with_the_parsed_errors_and_the_request_id()
    {
        // Arrange
        Host.Api.Enqueue(Rejection(HttpStatusCode.UnprocessableContent));

        // Act
        var act = () => Emails.CreateAsync(Organisation.Id, EmailCreateOptions(), cancellationToken: Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.UnprocessableContent);
        exception.RequestId.Should().Be(Request.Id);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Source!.Pointer.Should().Be(Error.Pointer);
    }

    [Fact]
    public async Task When_the_rate_limit_is_reached_ListAsync_throws_with_the_retry_delay()
    {
        // Arrange
        Host.Api.Enqueue(new(HttpStatusCode.TooManyRequests) { Headers = { RetryAfter = new(Request.RetryAfter) } });

        // Act
        var act = () => Emails.ListAsync(Organisation.Id, cancellationToken: Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.TooManyRequests);
        exception.Errors.Should().BeEmpty();
        exception.RetryAfter.Should().Be(Request.RetryAfter);
    }
}
