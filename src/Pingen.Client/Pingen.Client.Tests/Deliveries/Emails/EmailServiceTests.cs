using System.Net;
using System.Text;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Deliveries.Emails;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests.Deliveries.Emails;

public class EmailServiceTests
{
    private static readonly Guid OrganisationId = Guid.Parse("6c3d1f0a-1111-4000-8000-000000000001");
    private static readonly Guid EmailId = Guid.Parse("934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e");
    private static readonly Guid EventId = Guid.Parse("7b1f9c22-3333-4000-8000-000000000003");
    private const string Prefix = "/organisations/6c3d1f0a-1111-4000-8000-000000000001/deliveries/emails";
    private const string EmailPath = $"{Prefix}/934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e";

    private const string Abilities = """
                                     "meta": { "abilities": { "self": { "cancel": "ok", "delete": "state" } } },
                                     """;

    private static string EmailJson(string meta = "") =>
        $$"""
          {
            {{meta}}
            "id": "934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e",
            "type": "emails",
            "attributes": {
              "status": "sent",
              "file_original_name": "rechnung_zürich.pdf",
              "file_pages": 2,
              "recipient_identifier": "info@acme.com",
              "price_currency": "CHF",
              "price_value": 1.25,
              "source": "api",
              "submitted_at": "2021-11-19T09:42:48+0100",
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2020-11-20T10:00:00+01:00"
            },
            "relationships": {
              "organisation": {
                "links": { "related": "https://api.pingen.com/organisations/6c3d1f0a" },
                "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" }
              },
              "batch": { "data": null },
              "events": {
                "links": { "related": { "href": "https://api.pingen.com/emails/934b6a01/events", "meta": { "count": 3 } } }
              }
            },
            "links": { "self": "https://api.pingen.com/emails/934b6a01" }
          }
          """;

    private static string SingleJson => $$"""{ "data": {{EmailJson(Abilities)}} }""";

    private const string EventsJson = """
                                      {
                                        "data": [
                                          {
                                            "id": "7b1f9c22-3333-4000-8000-000000000003",
                                            "type": "deliverables_events",
                                            "attributes": {
                                              "code": "delivered",
                                              "name": "Zugestellt",
                                              "producer": "Pingen",
                                              "location": "8051 Zürich, CH",
                                              "has_image": true,
                                              "data": [],
                                              "emitted_at": "2021-11-19T09:42:48+0100",
                                              "created_at": "2021-11-19T09:42:48+0100",
                                              "updated_at": "2021-11-19T09:42:48+0100"
                                            },
                                            "relationships": {
                                              "email": { "data": { "id": "934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e", "type": "emails" } }
                                            }
                                          }
                                        ],
                                        "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
                                      }
                                      """;

    private const string UploadJson = """
                                      {
                                        "data": {
                                          "id": "1a2b3c4d-5555-4000-8000-000000000005",
                                          "type": "file_uploads",
                                          "attributes": {
                                            "url": "https://s3.example.com/bucket/1a2b3c4d.pdf?signer=url",
                                            "url_signature": "$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV",
                                            "expires_at": "2021-11-19T09:42:48+0100"
                                          }
                                        }
                                      }
                                      """;

    private static readonly byte[] Pdf = "%PDF-1.7 Zürich"u8.ToArray();

    private static EmailCreateOptions CreateOptions => new()
    {
        FileOriginalName = "rechnung_zürich.pdf",
        AutoSend = true,
        MetaData = new()
        {
            SenderName = "ACME AG",
            RecipientEmail = "info@acme.com",
            RecipientName = "ACME AG",
            ReplyEmail = "reply@acme.com",
            ReplyName = "ACME Support",
            Subject = "Ihre Rechnung",
            Content = "Guten Tag\n\nAnbei Ihre Rechnung.",
        },
    };

    private static string ListJson(int currentPage, int lastPage) =>
        $$"""
          {
            "data": [ {{EmailJson()}} ],
            "links": { "self": "https://api.pingen.com/emails?page[number]={{currentPage}}" },
            "meta": { "current_page": {{currentPage}}, "last_page": {{lastPage}}, "per_page": 1, "from": 1, "to": 1, "total": {{lastPage}} }
          }
          """;

    [Fact]
    public async Task When_options_shape_the_page_ListAsync_gets_the_delivery_list_with_the_query_and_maps_the_page()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(ListJson(currentPage: 1, lastPage: 1));

        // Act
        var emails = await new EmailService(host.Client).ListAsync(
            OrganisationId,
            new()
            {
                PageNumber = 2,
                PageLimit = 50,
                Sort = "-created_at",
                Filter = PingenFilter.Where("status", "sent"),
                Search = "Zürich",
                Include = "organisation",
                Fields = new Dictionary<string, string> { ["emails"] = "status,created_at" },
            },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(Prefix);
        host.Api.Request.Query.Should().Be(
            "?page[number]=2&page[limit]=50&sort=-created_at&filter=%7B%22status%22%3A%22sent%22%7D&q=Z%C3%BCrich&include=organisation&fields[emails]=status,created_at"
        );
        var email = emails.Should().ContainSingle().Which;
        email.Id.Should().Be(EmailId);
        email.Type.Should().Be("emails");
        email.Attributes.Status.Should().Be("sent");
        email.Attributes.FileOriginalName.Should().Be("rechnung_zürich.pdf");
        email.Attributes.FilePages.Should().Be(2);
        email.Attributes.RecipientIdentifier.Should().Be("info@acme.com");
        email.Attributes.PriceValue.Should().Be(1.25m);
        email.Attributes.SubmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        email.Attributes.UpdatedAt.Should().Be(new DateTimeOffset(2020, 11, 20, 10, 0, 0, TimeSpan.FromHours(1)));
        email.Relationships!.Organisation!.Data!.Id.Should().Be("6c3d1f0a-1111-4000-8000-000000000001");
        email.Relationships.Batch!.Data.Should().BeNull();
        email.Relationships.Events!.Count.Should().Be(3);
        email.Meta.Should().BeNull();
        emails.Meta!.Total.Should().Be(1);
        emails.Links!.Self.Should().Be("https://api.pingen.com/emails?page[number]=1");
    }

    [Fact]
    public async Task When_the_collection_spans_pages_ListAutoPagingAsync_yields_every_item_and_asks_for_the_next_page()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(ListJson(currentPage: 1, lastPage: 2)).EnqueueOk(ListJson(currentPage: 2, lastPage: 2));

        // Act
        var emails = await new EmailService(host.Client)
            .ListAutoPagingAsync(OrganisationId, new() { PageLimit = 1 }, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        emails.Should().HaveCount(2);
        host.Api.Requests.Should().HaveCount(2);
        host.Api.Requests[0].Query.Should().Be("?page[limit]=1");
        host.Api.Requests[1].Query.Should().Be("?page[number]=2&page[limit]=1");
    }

    [Fact]
    public async Task When_a_file_is_already_uploaded_CreateAsync_posts_the_envelope_without_an_id_and_with_the_preset()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueJson(HttpStatusCode.Created, SingleJson);
        var presetId = Guid.Parse("2f8e4d10-2222-4000-8000-000000000002");

        // Act
        var email = await new EmailService(host.Client).CreateAsync(
            OrganisationId,
            CreateOptions with
            {
                FileUrl = "https://s3.example.com/bucket/1a2b3c4d.pdf?signer=url",
                FileUrlSignature = "$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV",
                PresetId = presetId,
            },
            new() { IdempotencyKey = "3f0d9e21-4444-4000-8000-000000000004" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Post);
        host.Api.Request.Path.Should().Be(Prefix);
        host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        host.Api.Request.Header("Idempotency-Key").Should().Be("3f0d9e21-4444-4000-8000-000000000004");
        var data = host.Api.Request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("emails");
        data.TryGetProperty("id", out _).Should().BeFalse();
        data.GetProperty("relationships").GetProperty("preset").GetProperty("data").GetProperty("id").GetString().Should().Be(presetId.ToString());
        var attributes = data.GetProperty("attributes");
        attributes.GetProperty("file_original_name").GetString().Should().Be("rechnung_zürich.pdf");
        attributes.GetProperty("auto_send").GetBoolean().Should().BeTrue();
        attributes.TryGetProperty("PresetId", out _).Should().BeFalse();
        var metaData = attributes.GetProperty("meta_data");
        metaData.GetProperty("sender_name").GetString().Should().Be("ACME AG");
        metaData.GetProperty("recipient_email").GetString().Should().Be("info@acme.com");
        metaData.GetProperty("reply_name").GetString().Should().Be("ACME Support");
        metaData.GetProperty("subject").GetString().Should().Be("Ihre Rechnung");
        metaData.GetProperty("content").GetString().Should().Be("Guten Tag\n\nAnbei Ihre Rechnung.");
        email.Id.Should().Be(EmailId);
        email.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("cancel", "ok"));
    }

    [Fact]
    public async Task When_no_url_is_known_yet_CreateAsync_uploads_the_stream_and_copies_the_url_and_signature_into_the_create_call()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(UploadJson).EnqueueJson(HttpStatusCode.Created, SingleJson);
        host.Files.EnqueueEmpty(HttpStatusCode.OK);
        using var content = new MemoryStream(Pdf);

        // Act
        await new EmailService(host.Client).CreateAsync(OrganisationId, content, CreateOptions, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        host.Api.Requests[0].Path.Should().Be("/file-upload");
        host.Files.Request.Method.Should().Be(HttpMethod.Put);
        host.Files.Request.Url.Should().Be(new Uri("https://s3.example.com/bucket/1a2b3c4d.pdf?signer=url"));
        host.Files.Request.Body.Should().Equal(Pdf);
        host.Files.Request.Header("Authorization").Should().BeNull();
        host.Api.Requests[1].Method.Should().Be(HttpMethod.Post);
        host.Api.Requests[1].Path.Should().Be(Prefix);
        var attributes = host.Api.Requests[1].Json.GetProperty("data").GetProperty("attributes");
        attributes.GetProperty("file_url").GetString().Should().Be("https://s3.example.com/bucket/1a2b3c4d.pdf?signer=url");
        attributes.GetProperty("file_url_signature").GetString().Should().Be("$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV");
    }

    [Fact]
    public async Task When_the_options_already_carry_an_upload_CreateAsync_with_a_stream_refuses_to_overwrite_it()
    {
        // Arrange
        using var host = new PingenTestHost();
        using var content = new MemoryStream(Pdf);

        // Act
        var act = () => new EmailService(host.Client).CreateAsync(
            OrganisationId,
            content,
            CreateOptions with { FileUrl = "https://s3.example.com/bucket/other.pdf" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("options");
        host.Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_one_email_is_addressed_GetAsync_gets_it_and_maps_the_abilities()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(SingleJson);

        // Act
        var email = await new EmailService(host.Client).GetAsync(OrganisationId, EmailId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(EmailPath);
        host.Api.Request.Query.Should().BeEmpty();
        email.Meta!.Abilities.Should().BeEquivalentTo(new Dictionary<string, string> { ["cancel"] = "ok", ["delete"] = "state" });
    }

    [Fact]
    public async Task When_an_email_is_dropped_DeleteAsync_deletes_it_without_a_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty();

        // Act
        await new EmailService(host.Client).DeleteAsync(OrganisationId, EmailId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Delete);
        host.Api.Request.Path.Should().Be(EmailPath);
        host.Api.Request.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task When_an_email_is_still_cancellable_CancelAsync_patches_cancel_without_a_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await new EmailService(host.Client).CancelAsync(
            OrganisationId,
            EmailId,
            new() { IdempotencyKey = "3f0d9e21-4444-4000-8000-000000000004" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Path.Should().Be($"{EmailPath}/cancel");
        host.Api.Request.Body.Should().BeEmpty();
        host.Api.Request.Header("Idempotency-Key").Should().Be("3f0d9e21-4444-4000-8000-000000000004");
    }

    [Fact]
    public async Task When_the_file_endpoint_redirects_DownloadFileAsync_follows_the_location_without_authentication()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(new(HttpStatusCode.Found) { Headers = { Location = new("https://s3.example.com/bucket/934b6a01.pdf?signer=url") } });
        host.Files.Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) });

        // Act
        await using var file = await new EmailService(host.Client).DownloadFileAsync(OrganisationId, EmailId, TestContext.Current.CancellationToken);

        // Assert
        using var downloaded = new MemoryStream();
        await file.CopyToAsync(downloaded, TestContext.Current.CancellationToken);
        downloaded.ToArray().Should().Equal(Pdf);
        host.Api.Request.Path.Should().Be($"{EmailPath}/file");
        host.Files.Request.Url.Should().Be(new Uri("https://s3.example.com/bucket/934b6a01.pdf?signer=url"));
        host.Files.Request.Header("Authorization").Should().BeNull();
    }

    [Fact]
    public async Task When_the_events_of_an_email_are_listed_ListEventsAsync_gets_them_localized_and_maps_the_parent()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(EventsJson);

        // Act
        var events = await new EmailService(host.Client).ListEventsAsync(
            OrganisationId,
            EmailId,
            new() { Language = "de-DE" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Path.Should().Be($"{EmailPath}/events");
        host.Api.Request.Query.Should().Be("?language=de-DE");
        var @event = events.Should().ContainSingle().Which;
        @event.Type.Should().Be("deliverables_events");
        @event.Attributes.Name.Should().Be("Zugestellt");
        @event.Relationships!.Parent!.Data!.Type.Should().Be("emails");
    }

    [Fact]
    public async Task When_an_event_carries_an_image_GetEventImageLocationAsync_returns_the_location_instead_of_following_it()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(new(HttpStatusCode.Found) { Headers = { Location = new("https://s3.example.com/bucket/event.png?signer=url") } });

        // Act
        var location = await new EmailService(host.Client).GetEventImageLocationAsync(OrganisationId, EmailId, EventId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Path.Should().Be($"{EmailPath}/events/{EventId}/image");
        location.Should().Be(new Uri("https://s3.example.com/bucket/event.png?signer=url"));
        host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_an_event_image_is_downloaded_DownloadEventImageAsync_streams_the_redirect_target()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(new(HttpStatusCode.Found) { Headers = { Location = new("https://s3.example.com/bucket/event.png?signer=url") } });
        host.Files.Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) });

        // Act
        await using var image = await new EmailService(host.Client).DownloadEventImageAsync(OrganisationId, EmailId, EventId, TestContext.Current.CancellationToken);

        // Assert
        using var downloaded = new MemoryStream();
        await image.CopyToAsync(downloaded, TestContext.Current.CancellationToken);
        downloaded.ToArray().Should().Equal(Pdf);
        host.Files.Request.Header("Authorization").Should().BeNull();
    }

    [Fact]
    public async Task When_the_create_call_is_rejected_CreateAsync_throws_with_the_parsed_errors_and_the_request_id()
    {
        // Arrange
        using var host = new PingenTestHost();
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableContent)
        {
            Content = new StringContent(
                """{ "errors": [ { "code": "22", "title": "Validation error", "detail": "The meta data is incomplete", "source": { "pointer": "/data/attributes/meta_data" } } ] }""",
                Encoding.UTF8,
                PingenClient.JsonApiMediaType
            ),
        };
        response.Headers.Add("X-Request-Id", "cf1c0f4b-6666-4000-8000-000000000006");
        host.Api.Enqueue(response);

        // Act
        var act = () => new EmailService(host.Client).CreateAsync(OrganisationId, CreateOptions, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        exception.RequestId.Should().Be("cf1c0f4b-6666-4000-8000-000000000006");
        exception.Errors.Should().ContainSingle().Which.Source!.Pointer.Should().Be("/data/attributes/meta_data");
    }

    [Fact]
    public async Task When_the_rate_limit_is_reached_ListAsync_throws_with_the_retry_delay()
    {
        // Arrange
        using var host = new PingenTestHost();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.Add("Retry-After", "42");
        host.Api.Enqueue(response);

        // Act
        var act = () => new EmailService(host.Client).ListAsync(OrganisationId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(42));
        exception.Errors.Should().BeEmpty();
    }
}
