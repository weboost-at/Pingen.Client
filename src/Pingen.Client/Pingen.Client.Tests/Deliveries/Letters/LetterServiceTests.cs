using System.Net;
using System.Text;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Deliveries.ValueTypes;
using Pingen.Client.Tests.Deliveries.Letters.Support;

namespace Pingen.Client.Tests.Deliveries.Letters;

public class LetterServiceTests : LetterTestBase
{
    [Fact]
    public async Task When_a_page_is_requested_ListAsync_sends_the_query_and_maps_the_page()
    {
        // Arrange
        Host.Api.EnqueueOk(LetterPage(number: 1, lastPage: 2));

        var options = new PingenListOptions
        {
            PageNumber = 1,
            PageLimit = 1,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "sent"),
            Fields = new Dictionary<string, string> { ["letters"] = "status,created_at" }
        };

        // Act
        var page = await Letters.ListAsync(Organisation.Id, options, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, LettersPath);
        Host.Api.Request.Assert.Query(
            """?page[number]=1&page[limit]=1&sort=-created_at&filter={"status":"sent"}&fields[letters]=status,created_at""");

        page.Should().ContainSingle();
        page[0].Meta.Should().BeNull();
        page[0].Attributes.Status.Should().Be("sent");
        page.Meta!.Total.Should().Be(2);
        page.Links!.Next.Should().Be($"https://api.pingen.com{LettersPath}?page[number]=2");
    }

    [Fact]
    public async Task When_two_pages_are_enumerated_ListAutoPagingAsync_yields_both_and_stops_at_the_last()
    {
        // Arrange
        Host.Api.EnqueueOk(LetterPage(number: 1, lastPage: 2)).EnqueueOk(LetterPage(number: 2, lastPage: 2));

        // Act
        var letters = await Letters
            .ListAutoPagingAsync(Organisation.Id, new() { PageLimit = 1, Sort = "-created_at" }, Ct)
            .ToListAsync(Ct);

        // Assert
        letters.Select(letter => letter.Attributes.FilePages).Should().Equal(1, 2);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[0].Assert.Query("?page[limit]=1&sort=-created_at");
        Host.Api.Requests[1].Assert.Query("?page[number]=2&page[limit]=1&sort=-created_at");
    }

    [Fact]
    public async Task When_the_file_is_uploaded_already_CreateAsync_posts_the_envelope_with_preset_and_key()
    {
        // Arrange
        Host.Api.EnqueueJson(HttpStatusCode.Created, LetterDocument());

        var options = new LetterCreateOptions
        {
            FileOriginalName = Letter.FileOriginalName,
            FileUrl = File.Url.ToString(),
            FileUrlSignature = File.Signature,
            AutoSend = true,
            AddressPosition = AddressPosition.Right,
            DeliveryProduct = DeliveryProduct.Cheap,
            PrintMode = PrintMode.Duplex,
            PrintSpectrum = PrintSpectrum.Grayscale,
            PresetId = Letter.PresetId
        };

        // Act
        var letter = await Letters.CreateAsync(Organisation.Id, options, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        letter.Id.Should().Be(Letter.Id);

        Host.Api.Request.Assert.Sent(HttpMethod.Post, LettersPath);
        Host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);

        Host.Api.Request.Data.Assert.Lacks("id");
        Host.Api.Request.Data.Assert.Has("type", "letters");
        Host.Api.Request.Attributes.Assert.Lacks("meta_data");
        Host.Api.Request.Attributes.Assert.Has("delivery_product", "cheap");
        Host.Api.Request.Attributes.Assert.Has("address_position", "right");
        Host.Api.Request.Attributes.Assert.Has("file_original_name", Letter.FileOriginalName);

        var preset = Host.Api.Request.Data.GetProperty("relationships").GetProperty("preset").GetProperty("data");
        preset.Assert.Has("type", "presets");
        preset.Assert.Has("id", Letter.PresetId.ToString());
    }

    [Fact]
    public async Task When_a_stream_is_given_CreateAsync_uploads_it_and_posts_the_presigned_url()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument()).EnqueueJson(HttpStatusCode.Created, LetterDocument());
        Host.Files.EnqueueEmpty(HttpStatusCode.OK);

        using var content = new MemoryStream(File.Pdf);

        // Act
        var letter = await Letters.CreateAsync(
            Organisation.Id,
            content,
            new() { FileOriginalName = Letter.FileOriginalName, AutoSend = false },
            cancellationToken: Ct
        );

        // Assert
        letter.Id.Should().Be(Letter.Id);

        Host.Api.Requests[0].Path.Should().Be("/file-upload");

        Host.Files.Request.Body.Should().Equal(File.Pdf);
        Host.Files.Request.Url.Should().Be(File.Url);
        Host.Files.Request.Method.Should().Be(HttpMethod.Put);
        Host.Files.Request.Assert.Unauthenticated();
        Host.Files.Request.Header("Content-Type").Should().BeNull();

        Host.Api.Requests[1].Path.Should().Be(LettersPath);
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

        var options = new LetterCreateOptions
        {
            FileOriginalName = Letter.FileOriginalName,
            AutoSend = false,
            FileUrl = url ? File.Url.ToString() : null,
            FileUrlSignature = signature ? File.Signature : null
        };

        // Act
        var act = () => Letters.CreateAsync(Organisation.Id, content, options, cancellationToken: Ct);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("options")
            .WithMessage("*fills FileUrl and FileUrlSignature - leave both unset*");

        Host.Api.Requests.Should().BeEmpty();
        Host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_single_letter_is_requested_GetAsync_gets_its_path_and_maps_the_abilities()
    {
        // Arrange
        Host.Api.EnqueueOk(LetterDocument());

        // Act
        var letter = await Letters.GetAsync(Organisation.Id, Letter.Id, Ct);

        // Assert
        Host.Api.Request.Query.Should().BeEmpty();
        Host.Api.Request.Assert.Sent(HttpMethod.Get, LetterPath);
        Host.Api.Request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");

        letter.Meta!.Abilities!["cancel"].Should().Be("ok");
    }

    [Fact]
    public async Task When_a_letter_is_deleted_DeleteAsync_sends_a_bodyless_delete_to_its_path()
    {
        // Arrange
        Host.Api.EnqueueEmpty();

        // Act
        await Letters.DeleteAsync(Organisation.Id, Letter.Id, Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Delete, LetterPath);
    }

    [Fact]
    public async Task When_a_letter_is_cancelled_CancelAsync_patches_the_cancel_path_without_a_body()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await Letters.CancelAsync(Organisation.Id, Letter.Id, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{LetterPath}/cancel");
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);
    }

    [Fact]
    public async Task When_a_letter_is_sent_SendAsync_patches_the_send_path_repeating_the_id_in_the_body()
    {
        // Arrange
        Host.Api.EnqueueOk(LetterDocument());

        var options = new LetterSendOptions
        {
            DeliveryProduct = DeliveryProduct.Premium,
            PrintMode = PrintMode.Simplex,
            PrintSpectrum = PrintSpectrum.Color,
            MetaData = Letter.MetaData
        };

        // Act
        var letter = await Letters.SendAsync(Organisation.Id, Letter.Id, options, cancellationToken: Ct);

        // Assert
        letter.Attributes.Status.Should().Be("sent");

        Host.Api.Request.Assert.Sent(HttpMethod.Patch, $"{LetterPath}/send");
        Host.Api.Request.Data.Assert.Has("type", "letters");
        Host.Api.Request.Data.Assert.Has("id", Letter.Id.ToString());
        Host.Api.Request.Attributes.Assert.Has("delivery_product", "premium");

        var metaData = Host.Api.Request.Attributes.GetProperty("meta_data");
        metaData.GetProperty("recipient").Assert.Lacks("street");
        metaData.GetProperty("recipient").Assert.Has("pobox", Letter.MetaData.Recipient.PoBox);
        metaData.GetProperty("sender").Assert.Has("city", Letter.MetaData.Sender.City);
    }

    [Fact]
    public async Task When_the_file_is_asked_for_GetFileLocationAsync_returns_the_location_unfollowed()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);

        // Act
        var location = await Letters.GetFileLocationAsync(Organisation.Id, Letter.Id, Ct);

        // Assert
        location.Should().Be(File.Location);

        Host.Files.Requests.Should().BeEmpty();
        Host.Api.Requests.Should().ContainSingle();
        Host.Api.Requests[0].Path.Should().Be($"{LetterPath}/file");
    }

    [Fact]
    public async Task When_the_file_is_downloaded_DownloadFileAsync_fetches_the_location_without_authentication()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);
        Host.Files.EnqueueBytes(File.Pdf);

        using var downloaded = new MemoryStream();

        // Act
        await using var file = await Letters.DownloadFileAsync(Organisation.Id, Letter.Id, Ct);
        await file.CopyToAsync(downloaded, Ct);

        // Assert
        downloaded.ToArray().Should().Equal(File.Pdf);

        Host.Api.Request.Path.Should().Be($"{LetterPath}/file");

        Host.Files.Request.Url.Should().Be(File.Location);
        Host.Files.Request.Assert.Unauthenticated();
    }

    [Fact]
    public async Task When_the_events_of_a_letter_are_listed_ListEventsAsync_sends_the_language()
    {
        // Arrange
        Host.Api.EnqueueOk(EventsDocument());

        // Act
        var events = await Letters.ListEventsAsync(Organisation.Id, Letter.Id, new() { Language = "de-DE", Sort = "-emitted_at" }, Ct);

        // Assert
        Host.Api.Request.Path.Should().Be($"{LetterPath}/events");
        Host.Api.Request.Assert.Query("?sort=-emitted_at&language=de-DE");

        events.Should().ContainSingle();
        events[0].Type.Should().Be("letters_events");
        events[0].Attributes.Code.Should().Be("undeliverable");
        events[0].Relationships!.Parent!.Data!.Type.Should().Be("letters");
    }

    [Fact]
    public async Task When_an_event_image_is_downloaded_its_location_is_fetched_without_authentication()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);
        Host.Files.EnqueueBytes(File.Pdf);

        // Act
        await using var image = await Letters.DownloadEventImageAsync(Organisation.Id, Letter.Id, Letter.EventId, Ct);

        // Assert
        image.Should().NotBeNull();

        Host.Api.Request.Path.Should().Be($"{LetterPath}/events/{Letter.EventId}/image");

        Host.Files.Request.Url.Should().Be(File.Location);
        Host.Files.Request.Assert.Unauthenticated();
    }

    [Fact]
    public async Task When_an_event_image_is_asked_for_GetEventImageLocationAsync_returns_the_location()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);

        // Act
        var location = await Letters.GetEventImageLocationAsync(Organisation.Id, Letter.Id, Letter.EventId, Ct);

        // Assert
        location.Should().Be(File.Location);

        Host.Api.Request.Path.Should().Be($"{LetterPath}/events/{Letter.EventId}/image");
    }

    [Theory]
    [InlineData("sent")]
    [InlineData("delivered")]
    [InlineData("issues")]
    [InlineData("undeliverable")]
    public async Task When_a_sort_is_given_the_organisation_wide_event_lists_drop_it(string category)
    {
        // Arrange
        Host.Api.EnqueueOk(EventsDocument());

        var options = new PingenListOptions { Sort = "-emitted_at", PageLimit = 100 };

        // Act
        var events = await (category switch
        {
            "sent" => Letters.ListSentEventsAsync(Organisation.Id, options, Ct),
            "delivered" => Letters.ListDeliveredEventsAsync(Organisation.Id, options, Ct),
            "issues" => Letters.ListIssueEventsAsync(Organisation.Id, options, Ct),
            _ => Letters.ListUndeliverableEventsAsync(Organisation.Id, options, Ct)
        });

        // Assert
        events.Should().ContainSingle();

        Host.Api.Request.Assert.Query("?page[limit]=100");
        Host.Api.Request.Assert.Sent(HttpMethod.Get, $"{LettersPath}/events/{category}");
    }

    [Fact]
    public async Task When_the_price_is_calculated_CalculatePriceAsync_posts_the_envelope_and_maps_the_price()
    {
        // Arrange
        Host.Api.EnqueueOk(PriceDocument());

        var options = new LetterPriceOptions
        {
            Country = "CH",
            PaperTypes = [PaperType.Normal, PaperType.Qr],
            PrintMode = PrintMode.Simplex,
            PrintSpectrum = PrintSpectrum.Color,
            DeliveryProduct = DeliveryProduct.Fast
        };

        // Act
        var price = await Letters.CalculatePriceAsync(Organisation.Id, options, cancellationToken: Ct);

        // Assert
        price!.Attributes.Currency.Should().Be("EUR");
        price.Attributes.Price.Should().Be(12.12m);

        Host.Api.Request.Data.Assert.Has("type", "letter_price_calculator");
        Host.Api.Request.Assert.Sent(HttpMethod.Post, $"{LettersPath}/price-calculator");
        Host.Api.Request.Attributes
            .GetProperty("paper_types")
            .EnumerateArray()
            .Select(type => type.GetString())
            .Should().Equal("normal", "qr");
    }

    [Fact]
    public async Task When_the_price_is_not_ready_CalculatePriceAsync_maps_the_empty_accepted_answer_to_null()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        var options = new LetterPriceOptions
        {
            Country = "CH",
            PaperTypes = [PaperType.Normal],
            PrintMode = PrintMode.Simplex,
            PrintSpectrum = PrintSpectrum.Color,
            DeliveryProduct = DeliveryProduct.Fast
        };

        // Act
        var price = await Letters.CalculatePriceAsync(Organisation.Id, options, cancellationToken: Ct);

        // Assert
        price.Should().BeNull();

        Host.Api.Request.Path.Should().Be($"{LettersPath}/price-calculator");
    }

    [Fact]
    public async Task When_the_api_rejects_the_letter_CreateAsync_throws_with_the_errors_and_the_request_id()
    {
        // Arrange
        Host.Api.Enqueue(Rejection(HttpStatusCode.UnprocessableEntity));

        // Act
        var act = () => Letters.CreateAsync(
            Organisation.Id,
            new() { FileOriginalName = Letter.FileOriginalName, AutoSend = true },
            cancellationToken: Ct
        );

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.UnprocessableEntity);
        exception.Message.Should().Contain("422");
        exception.RequestId.Should().Be(Request.Id);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Title.Should().Be(Error.Title);
        exception.Errors[0].Source!.Pointer.Should().Be(Error.Pointer);
    }

    [Fact]
    public async Task When_the_organisation_is_rate_limited_ListAsync_throws_with_the_retry_delay()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("<html>slow down</html>", Encoding.UTF8, "text/html"),
            Headers = { RetryAfter = new(Request.RetryAfter) }
        };

        Host.Api.Enqueue(response);

        // Act
        var act = () => Letters.ListAsync(Organisation.Id, cancellationToken: Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.TooManyRequests);
        exception.Errors.Should().BeEmpty();
        exception.RetryAfter.Should().Be(Request.RetryAfter);
    }
}
