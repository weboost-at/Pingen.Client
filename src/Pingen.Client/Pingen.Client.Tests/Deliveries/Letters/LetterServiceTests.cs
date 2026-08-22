using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Deliveries.ValueTypes;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests.Deliveries.Letters;

public class LetterServiceTests
{
    private static readonly Guid Organisation = Guid.Parse("6c3d1f0a-1111-4000-8000-000000000001");

    private static readonly Guid LetterId = Guid.Parse("2a4c9e77-2222-4000-8000-000000000002");

    private static readonly Guid EventId = Guid.Parse("934b6a01-3333-4000-8000-000000000003");

    private static readonly Guid PresetId = Guid.Parse("7f0b2c55-4444-4000-8000-000000000004");

    private const string LettersPath = "/organisations/6c3d1f0a-1111-4000-8000-000000000001/deliveries/letters";

    private const string LetterPath = $"{LettersPath}/2a4c9e77-2222-4000-8000-000000000002";

    private static readonly byte[] Pdf = "%PDF-1.7 Zürich"u8.ToArray();

    [Fact]
    public async Task When_a_page_is_requested_ListAsync_gets_the_letters_path_with_the_query_and_maps_the_page()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(PageJson(1, 2));
        var options = new PingenListOptions
        {
            PageNumber = 1,
            PageLimit = 1,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "sent"),
            Fields = new Dictionary<string, string> { ["letters"] = "status,created_at" },
        };

        // Act
        var page = await new LetterService(host.Client).ListAsync(Organisation, options, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(LettersPath);
        Uri.UnescapeDataString(host.Api.Request.Query).Should()
            .Be("""?page[number]=1&page[limit]=1&sort=-created_at&filter={"status":"sent"}&fields[letters]=status,created_at""");
        page.Should().ContainSingle().Which.Attributes.Status.Should().Be("sent");
        page[0].Meta.Should().BeNull();
        page.Meta!.Total.Should().Be(2);
        page.Links!.Next.Should().Be($"https://api.pingen.com{LettersPath}?page[number]=2");
    }

    [Fact]
    public async Task Given_a_collection_spanning_two_pages_When_it_is_enumerated_ListAutoPagingAsync_yields_every_letter_and_stops_at_the_last_page()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(PageJson(1, 2)).EnqueueOk(PageJson(2, 2));
        var options = new PingenListOptions { PageLimit = 1, Sort = "-created_at" };

        // Act
        var letters = await new LetterService(host.Client)
            .ListAutoPagingAsync(Organisation, options, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        letters.Select(letter => letter.Attributes.FilePages).Should().Equal(1, 2);
        host.Api.Requests.Should().HaveCount(2);
        Uri.UnescapeDataString(host.Api.Requests[0].Query).Should().Be("?page[limit]=1&sort=-created_at");
        Uri.UnescapeDataString(host.Api.Requests[1].Query).Should().Be("?page[number]=2&page[limit]=1&sort=-created_at");
    }

    [Fact]
    public async Task When_the_pdf_was_uploaded_already_CreateAsync_posts_the_letters_envelope_with_the_preset_and_the_idempotency_key()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueJson(HttpStatusCode.Created, SingleJson());

        // Act
        var letter = await new LetterService(host.Client).CreateAsync(
            Organisation,
            new()
            {
                FileOriginalName = "lörem.pdf",
                FileUrl = "https://s3.example.com/bucket/934b6a01.pdf?signer=url",
                FileUrlSignature = "$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV",
                AutoSend = true,
                AddressPosition = AddressPosition.Right,
                DeliveryProduct = DeliveryProduct.Cheap,
                PrintMode = PrintMode.Duplex,
                PrintSpectrum = PrintSpectrum.Grayscale,
                PresetId = PresetId,
            },
            new PingenRequestOptions { IdempotencyKey = "b3f1a2c4-5555-4000-8000-000000000005" },
            TestContext.Current.CancellationToken
        );

        // Assert
        var request = host.Api.Request;
        request.Method.Should().Be(HttpMethod.Post);
        request.Path.Should().Be(LettersPath);
        request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        request.Header("Idempotency-Key").Should().Be("b3f1a2c4-5555-4000-8000-000000000005");
        var data = request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("letters");
        data.TryGetProperty("id", out _).Should().BeFalse();
        data.GetProperty("attributes").GetProperty("file_original_name").GetString().Should().Be("lörem.pdf");
        data.GetProperty("attributes").GetProperty("address_position").GetString().Should().Be("right");
        data.GetProperty("attributes").GetProperty("delivery_product").GetString().Should().Be("cheap");
        data.GetProperty("attributes").TryGetProperty("meta_data", out _).Should().BeFalse();
        data.GetProperty("relationships").GetProperty("preset").GetProperty("data").GetProperty("id").GetString().Should().Be(PresetId.ToString());
        data.GetProperty("relationships").GetProperty("preset").GetProperty("data").GetProperty("type").GetString().Should().Be("presets");
        letter.Id.Should().Be(LetterId);
    }

    [Fact]
    public async Task When_a_stream_is_given_CreateAsync_requests_an_upload_target_puts_the_raw_bytes_and_posts_the_copied_url()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(UploadJson).EnqueueJson(HttpStatusCode.Created, SingleJson());
        host.Files.EnqueueEmpty(HttpStatusCode.OK);
        using var content = new MemoryStream(Pdf);

        // Act
        var letter = await new LetterService(host.Client).CreateAsync(
            Organisation,
            content,
            new() { FileOriginalName = "lörem.pdf", AutoSend = false },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Requests[0].Path.Should().Be("/file-upload");
        host.Files.Request.Method.Should().Be(HttpMethod.Put);
        host.Files.Request.Url.Should().Be(new Uri("https://s3.example.com/bucket/934b6a01.pdf?signer=url"));
        host.Files.Request.Body.Should().Equal(Pdf);
        host.Files.Request.Header("Authorization").Should().BeNull();
        host.Files.Request.Header("Content-Type").Should().BeNull();
        var attributes = host.Api.Requests[1].Json.GetProperty("data").GetProperty("attributes");
        host.Api.Requests[1].Path.Should().Be(LettersPath);
        attributes.GetProperty("file_url").GetString().Should().Be("https://s3.example.com/bucket/934b6a01.pdf?signer=url");
        attributes.GetProperty("file_url_signature").GetString().Should().Be("$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV");
        letter.Id.Should().Be(LetterId);
    }

    [Fact]
    public async Task When_the_options_already_carry_an_upload_target_the_stream_overload_of_CreateAsync_refuses_the_call()
    {
        // Arrange
        using var host = new PingenTestHost();
        using var content = new MemoryStream(Pdf);

        // Act
        var act = () => new LetterService(host.Client).CreateAsync(
            Organisation,
            content,
            new() { FileOriginalName = "lörem.pdf", AutoSend = false, FileUrl = "https://s3.example.com/bucket/934b6a01.pdf" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("options");
        host.Api.Requests.Should().BeEmpty();
        host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_single_letter_is_requested_GetAsync_gets_its_path_and_maps_the_abilities()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(SingleJson());

        // Act
        var letter = await new LetterService(host.Client).GetAsync(Organisation, LetterId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(LetterPath);
        host.Api.Request.Query.Should().BeEmpty();
        host.Api.Request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");
        letter.Meta!.Abilities!["cancel"].Should().Be("ok");
        letter.Attributes.Fonts.Should().ContainSingle().Which.IsEmbedded.Should().BeTrue();
    }

    [Fact]
    public async Task When_a_letter_is_deleted_DeleteAsync_sends_a_bodyless_delete_to_its_path()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty();

        // Act
        await new LetterService(host.Client).DeleteAsync(Organisation, LetterId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Delete);
        host.Api.Request.Path.Should().Be(LetterPath);
        host.Api.Request.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_letter_is_cancelled_CancelAsync_patches_the_cancel_path_without_a_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await new LetterService(host.Client).CancelAsync(
            Organisation,
            LetterId,
            new PingenRequestOptions { IdempotencyKey = "b3f1a2c4-5555-4000-8000-000000000005" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Path.Should().Be($"{LetterPath}/cancel");
        host.Api.Request.Body.Should().BeEmpty();
        host.Api.Request.Header("Idempotency-Key").Should().Be("b3f1a2c4-5555-4000-8000-000000000005");
    }

    [Fact]
    public async Task When_a_letter_is_sent_SendAsync_patches_the_send_path_with_the_id_repeated_in_the_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(SingleJson());

        // Act
        var letter = await new LetterService(host.Client).SendAsync(
            Organisation,
            LetterId,
            new()
            {
                DeliveryProduct = DeliveryProduct.Premium,
                PrintMode = PrintMode.Simplex,
                PrintSpectrum = PrintSpectrum.Color,
                MetaData = new()
                {
                    Recipient = new() { Name = "Alex Meier", PoBox = "Postfach 100", Zip = "8051", City = "Zürich", Country = "CH" },
                    Sender = new() { Name = "Pingen AG", Street = "Example street", Number = "50A", Zip = "8000", City = "Zürich", Country = "CH" },
                },
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Path.Should().Be($"{LetterPath}/send");
        var data = host.Api.Request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("letters");
        data.GetProperty("id").GetString().Should().Be(LetterId.ToString());
        data.GetProperty("attributes").GetProperty("delivery_product").GetString().Should().Be("premium");
        data.GetProperty("attributes").GetProperty("meta_data").GetProperty("recipient").GetProperty("pobox").GetString().Should().Be("Postfach 100");
        data.GetProperty("attributes").GetProperty("meta_data").GetProperty("recipient").TryGetProperty("street", out _).Should().BeFalse();
        data.GetProperty("attributes").GetProperty("meta_data").GetProperty("sender").GetProperty("city").GetString().Should().Be("Zürich");
        letter.Attributes.Status.Should().Be("sent");
    }

    [Fact]
    public async Task When_the_file_is_asked_for_GetFileLocationAsync_returns_the_location_header_without_following_it()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(Redirect());

        // Act
        var location = await new LetterService(host.Client).GetFileLocationAsync(Organisation, LetterId, TestContext.Current.CancellationToken);

        // Assert
        location.Should().Be(new Uri("https://s3.example.com/bucket/934b6a01.pdf?signer=url"));
        host.Api.Requests.Should().ContainSingle().Which.Path.Should().Be($"{LetterPath}/file");
        host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_the_file_is_downloaded_DownloadFileAsync_fetches_the_presigned_url_without_authentication()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(Redirect());
        host.Files.Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) });

        // Act
        await using var file = await new LetterService(host.Client).DownloadFileAsync(Organisation, LetterId, TestContext.Current.CancellationToken);

        // Assert
        using var downloaded = new MemoryStream();
        await file.CopyToAsync(downloaded, TestContext.Current.CancellationToken);
        downloaded.ToArray().Should().Equal(Pdf);
        host.Api.Request.Path.Should().Be($"{LetterPath}/file");
        host.Files.Request.Url.Should().Be(new Uri("https://s3.example.com/bucket/934b6a01.pdf?signer=url"));
        host.Files.Request.Header("Authorization").Should().BeNull();
    }

    [Fact]
    public async Task When_the_events_of_a_letter_are_listed_ListEventsAsync_gets_the_events_path_with_the_language()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(EventsJson);

        // Act
        var events = await new LetterService(host.Client).ListEventsAsync(
            Organisation,
            LetterId,
            new() { Language = "de-DE", Sort = "-emitted_at" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Path.Should().Be($"{LetterPath}/events");
        Uri.UnescapeDataString(host.Api.Request.Query).Should().Be("?sort=-emitted_at&language=de-DE");
        var @event = events.Should().ContainSingle().Which;
        @event.Type.Should().Be("letters_events");
        @event.Attributes.Code.Should().Be("undeliverable");
        @event.Relationships!.Parent!.Data!.Type.Should().Be("letters");
    }

    [Fact]
    public async Task When_an_event_image_is_downloaded_the_location_is_resolved_first_and_fetched_without_authentication()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(Redirect());
        host.Files.Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) });
        var service = new LetterService(host.Client);

        // Act
        await using var image = await service.DownloadEventImageAsync(Organisation, LetterId, EventId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Path.Should().Be($"{LetterPath}/events/{EventId}/image");
        host.Files.Request.Header("Authorization").Should().BeNull();
        image.Should().NotBeNull();
    }

    [Fact]
    public async Task When_an_event_image_location_is_asked_for_GetEventImageLocationAsync_returns_the_location_header()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(Redirect());

        // Act
        var location = await new LetterService(host.Client).GetEventImageLocationAsync(Organisation, LetterId, EventId, TestContext.Current.CancellationToken);

        // Assert
        location.Should().Be(new Uri("https://s3.example.com/bucket/934b6a01.pdf?signer=url"));
        host.Api.Request.Path.Should().Be($"{LetterPath}/events/{EventId}/image");
    }

    [Theory]
    [InlineData("sent")]
    [InlineData("delivered")]
    [InlineData("issues")]
    [InlineData("undeliverable")]
    public async Task When_a_sort_is_supplied_the_organisation_wide_event_lists_drop_it_since_the_endpoints_do_not_sort(string category)
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(EventsJson);
        var service = new LetterService(host.Client);
        var options = new PingenListOptions { Sort = "-emitted_at", PageLimit = 100 };

        // Act
        var events = await (category switch
        {
            "sent" => service.ListSentEventsAsync(Organisation, options, TestContext.Current.CancellationToken),
            "delivered" => service.ListDeliveredEventsAsync(Organisation, options, TestContext.Current.CancellationToken),
            "issues" => service.ListIssueEventsAsync(Organisation, options, TestContext.Current.CancellationToken),
            _ => service.ListUndeliverableEventsAsync(Organisation, options, TestContext.Current.CancellationToken),
        });

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be($"{LettersPath}/events/{category}");
        Uri.UnescapeDataString(host.Api.Request.Query).Should().Be("?page[limit]=100");
        events.Should().ContainSingle();
    }

    [Fact]
    public async Task When_the_price_is_calculated_CalculatePriceAsync_posts_the_calculator_envelope_and_maps_the_price()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(PriceJson);

        // Act
        var price = await new LetterService(host.Client).CalculatePriceAsync(
            Organisation,
            new()
            {
                Country = "CH",
                PaperTypes = [PaperType.Normal, PaperType.Qr],
                PrintMode = PrintMode.Simplex,
                PrintSpectrum = PrintSpectrum.Color,
                DeliveryProduct = DeliveryProduct.Fast,
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Post);
        host.Api.Request.Path.Should().Be($"{LettersPath}/price-calculator");
        var data = host.Api.Request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("letter_price_calculator");
        data.GetProperty("attributes").GetProperty("paper_types").EnumerateArray().Select(type => type.GetString()).Should().Equal("normal", "qr");
        price!.Attributes.Currency.Should().Be("EUR");
        price.Attributes.Price.Should().Be(12.12m);
    }

    [Fact]
    public async Task When_the_price_is_not_calculated_yet_CalculatePriceAsync_maps_the_empty_accepted_answer_to_null()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        var price = await new LetterService(host.Client).CalculatePriceAsync(
            Organisation,
            new()
            {
                Country = "CH",
                PaperTypes = [PaperType.Normal],
                PrintMode = PrintMode.Simplex,
                PrintSpectrum = PrintSpectrum.Color,
                DeliveryProduct = DeliveryProduct.Fast,
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        price.Should().BeNull();
        host.Api.Request.Path.Should().Be($"{LettersPath}/price-calculator");
    }

    [Fact]
    public async Task When_the_api_rejects_the_letter_CreateAsync_throws_with_the_parsed_errors_and_the_request_id()
    {
        // Arrange
        using var host = new PingenTestHost();
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent(ErrorsJson, Encoding.UTF8, PingenClient.JsonApiMediaType),
        };
        response.Headers.Add("X-Request-Id", "3d1f0a6c-6666-4000-8000-000000000006");
        host.Api.Enqueue(response);

        // Act
        var act = () => new LetterService(host.Client).CreateAsync(
            Organisation,
            new() { FileOriginalName = "lörem.pdf", AutoSend = true },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        exception.RequestId.Should().Be("3d1f0a6c-6666-4000-8000-000000000006");
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Title.Should().Be("The given data was invalid.");
        exception.Errors[0].Source!.Pointer.Should().Be("/data/attributes/file_url");
        exception.Message.Should().Contain("422");
    }

    [Fact]
    public async Task When_the_organisation_is_rate_limited_ListAsync_throws_with_the_retry_delay()
    {
        // Arrange
        using var host = new PingenTestHost();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("<html>slow down</html>", Encoding.UTF8, "text/html"),
        };
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
        host.Api.Enqueue(response);

        // Act
        var act = () => new LetterService(host.Client).ListAsync(Organisation, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
        exception.Errors.Should().BeEmpty();
    }

    private static HttpResponseMessage Redirect()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new("https://s3.example.com/bucket/934b6a01.pdf?signer=url");

        return response;
    }

    private static string SingleJson() => $$"""{ "data": {{LetterJson(2, withMeta: true)}} }""";

    private static string PageJson(int number, int lastPage) =>
        $$"""
          {
            "data": [ {{LetterJson(number, withMeta: false)}} ],
            "links": {
              "first": "https://api.pingen.com{{LettersPath}}?page[number]=1",
              "last": "https://api.pingen.com{{LettersPath}}?page[number]={{lastPage}}",
              "next": "https://api.pingen.com{{LettersPath}}?page[number]=2",
              "self": "https://api.pingen.com{{LettersPath}}?page[number]={{number}}"
            },
            "meta": { "current_page": {{number}}, "last_page": {{lastPage}}, "per_page": 1, "from": {{number}}, "to": {{number}}, "total": 2 }
          }
          """;

    private static string LetterJson(int pages, bool withMeta) =>
        $$"""
          {
            "id": "2a4c9e77-2222-4000-8000-000000000002",
            "type": "letters",
            "attributes": {
              "status": "sent",
              "file_original_name": "lörem.pdf",
              "file_pages": {{pages}},
              "address": "Hans Meier\nExample street 4\n8000 Zürich\nSwitzerland",
              "address_position": "left",
              "country": "CH",
              "delivery_product": "fast",
              "print_mode": "simplex",
              "print_spectrum": "color",
              "price_currency": "CHF",
              "price_value": 1.25,
              "paper_types": ["normal"],
              "fonts": [{ "name": "Helvetica", "is_embedded": 1 }],
              "source": "api",
              "tracking_number": null,
              "submitted_at": "",
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2020-11-20T10:00:00+0100"
            },
            "relationships": {
              "organisation": { "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" } },
              "batch": { "data": null },
              "events": { "links": { "related": { "href": "https://api.pingen.com/events", "meta": { "count": 3 } } } }
            },
            "links": { "self": "https://api.pingen.com{{LetterPath}}" }{{(withMeta ? AbilitiesJson : string.Empty)}}
          }
          """;

    private const string AbilitiesJson = """
                                         , "meta": { "abilities": { "self": { "cancel": "ok", "delete": "state" } } }
                                         """;

    private const string UploadJson = """
                                      {
                                        "data": {
                                          "id": "934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e",
                                          "type": "file_uploads",
                                          "attributes": {
                                            "url": "https://s3.example.com/bucket/934b6a01.pdf?signer=url",
                                            "url_signature": "$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV",
                                            "expires_at": "2021-11-19T09:42:48+0100"
                                          }
                                        }
                                      }
                                      """;

    private const string EventsJson = """
                                      {
                                        "data": [
                                          {
                                            "id": "934b6a01-3333-4000-8000-000000000003",
                                            "type": "letters_events",
                                            "attributes": {
                                              "code": "undeliverable",
                                              "name": "Nicht zustellbar",
                                              "producer": "Pingen",
                                              "location": "8051 Zürich, CH",
                                              "has_image": true,
                                              "data": ["moved"],
                                              "emitted_at": "2021-11-19T09:42:48+0100",
                                              "created_at": "2021-11-19T09:42:48+0100",
                                              "updated_at": "2021-11-19T09:42:48+0100"
                                            },
                                            "relationships": {
                                              "letter": { "data": { "id": "2a4c9e77-2222-4000-8000-000000000002", "type": "letters" } }
                                            }
                                          }
                                        ],
                                        "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
                                      }
                                      """;

    private const string PriceJson = """
                                     {
                                       "data": {
                                         "id": "7f0b2c55-4444-4000-8000-000000000004",
                                         "type": "letter_price_calculator",
                                         "attributes": { "currency": "EUR", "price": 12.12 },
                                         "links": { "self": "https://api.pingen.com/price" }
                                       }
                                     }
                                     """;

    private const string ErrorsJson = """
                                      {
                                        "errors": [
                                          {
                                            "code": "22",
                                            "title": "The given data was invalid.",
                                            "detail": "The file url signature is invalid.",
                                            "source": { "pointer": "/data/attributes/file_url", "parameter": null }
                                          }
                                        ]
                                      }
                                      """;
}
