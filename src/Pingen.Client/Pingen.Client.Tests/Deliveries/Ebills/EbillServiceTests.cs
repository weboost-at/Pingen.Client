using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Deliveries.Ebills;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests.Deliveries.Ebills;

public class EbillServiceTests
{
    private static readonly Guid OrganisationId = Guid.Parse("6c3d1f0a-1111-4000-8000-000000000001");
    private static readonly Guid EbillId = Guid.Parse("5d2c8b70-7777-4000-8000-000000000007");
    private static readonly Guid EventId = Guid.Parse("7b1f9c22-3333-4000-8000-000000000003");
    private const string Prefix = "/organisations/6c3d1f0a-1111-4000-8000-000000000001/deliveries/ebills";
    private const string EbillPath = $"{Prefix}/5d2c8b70-7777-4000-8000-000000000007";

    private const string Abilities = """
                                     "meta": { "abilities": { "self": { "send": "ok", "cancel": "state" } } },
                                     """;

    private static string EbillJson(string meta = "") =>
        $$"""
          {
            {{meta}}
            "id": "5d2c8b70-7777-4000-8000-000000000007",
            "type": "ebills",
            "attributes": {
              "status": "submitted",
              "file_original_name": "rechnung_zürich.pdf",
              "file_pages": 2,
              "recipient_identifier": "41100010014282213",
              "recipient_address": "ACME GmbH\nExamplestreet 432\n3000 Bern",
              "invoice_number": "Invoice 8051",
              "invoice_date": "2025-10-01",
              "invoice_due_date": "2025-10-30",
              "invoice_value": 1250.3,
              "invoice_currency": "CHF",
              "invoice_iban": "CH8009000000854254426",
              "invoice_address": "ACME GmbH\nExamplestreet 432\n3000 Bern",
              "invoice_reference": "111119346200000000000127257",
              "price_currency": "CHF",
              "price_value": 1.25,
              "source": "api",
              "submitted_at": null,
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2020-11-20T10:00:00+0100"
            },
            "relationships": {
              "organisation": { "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" } },
              "events": {
                "links": { "related": { "href": "https://api.pingen.com/ebills/5d2c8b70/events", "meta": { "count": 2 } } }
              }
            },
            "links": { "self": "https://api.pingen.com/ebills/5d2c8b70" }
          }
          """;

    private static string SingleJson => $$"""{ "data": {{EbillJson(Abilities)}} }""";

    private static string ListJson =>
        $$"""
          {
            "data": [ {{EbillJson()}} ],
            "links": { "self": "https://api.pingen.com/ebills" },
            "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
          }
          """;

    private const string EventsJson = """
                                      {
                                        "data": [
                                          {
                                            "id": "7b1f9c22-3333-4000-8000-000000000003",
                                            "type": "deliverables_events",
                                            "attributes": {
                                              "code": "approved",
                                              "name": "Approved",
                                              "producer": "Pingen",
                                              "location": "8051 Zürich, CH",
                                              "has_image": false,
                                              "data": [],
                                              "emitted_at": "2021-11-19T09:42:48+0100",
                                              "created_at": "2021-11-19T09:42:48+0100",
                                              "updated_at": "2021-11-19T09:42:48+0100"
                                            },
                                            "relationships": {
                                              "ebill": { "data": { "id": "5d2c8b70-7777-4000-8000-000000000007", "type": "ebills" } }
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

    private static EbillCreateOptions CreateOptions => new()
    {
        FileOriginalName = "rechnung_zürich.pdf",
        AutoSend = false,
        MetaData = new()
        {
            InvoiceNumber = "Invoice 8051",
            InvoiceDate = new(2025, 1, 1),
            InvoiceDueDate = new(2025, 1, 31),
            RecipientIdentifier = "41010560425610173",
        },
    };

    [Fact]
    public async Task When_the_ebills_of_an_organisation_are_listed_ListAsync_gets_them_and_maps_the_invoice_dates()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(ListJson);

        // Act
        var ebills = await new EbillService(host.Client).ListAsync(
            OrganisationId,
            new() { PageLimit = 100, Sort = "-created_at" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(Prefix);
        host.Api.Request.Query.Should().Be("?page[limit]=100&sort=-created_at");
        var ebill = ebills.Should().ContainSingle().Which;
        ebill.Id.Should().Be(EbillId);
        ebill.Type.Should().Be("ebills");
        ebill.Attributes.Status.Should().Be("submitted");
        ebill.Attributes.RecipientAddress.Should().Be("ACME GmbH\nExamplestreet 432\n3000 Bern");
        ebill.Attributes.InvoiceNumber.Should().Be("Invoice 8051");
        ebill.Attributes.InvoiceDate.Should().Be(new(2025, 10, 1));
        ebill.Attributes.InvoiceDueDate.Should().Be(new(2025, 10, 30));
        ebill.Attributes.InvoiceValue.Should().Be(1250.3m);
        ebill.Attributes.InvoiceIban.Should().Be("CH8009000000854254426");
        ebill.Attributes.InvoiceReference.Should().Be("111119346200000000000127257");
        ebill.Attributes.PriceValue.Should().Be(1.25m);
        ebill.Attributes.SubmittedAt.Should().BeNull();
        ebill.Relationships!.Events!.Count.Should().Be(2);
        ebill.Relationships.Batch.Should().BeNull();
        ebill.Meta.Should().BeNull();
        ebills.Meta!.Total.Should().Be(1);
    }

    [Fact]
    public async Task When_a_file_is_already_uploaded_CreateAsync_posts_the_envelope_with_the_invoice_dates_as_plain_days()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueJson(HttpStatusCode.Created, SingleJson);

        // Act
        var ebill = await new EbillService(host.Client).CreateAsync(
            OrganisationId,
            CreateOptions with
            {
                FileUrl = "https://s3.example.com/bucket/1a2b3c4d.pdf?signer=url",
                FileUrlSignature = "$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV",
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Post);
        host.Api.Request.Path.Should().Be(Prefix);
        host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        var data = host.Api.Request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("ebills");
        data.TryGetProperty("id", out _).Should().BeFalse();
        data.TryGetProperty("relationships", out _).Should().BeFalse();
        var attributes = data.GetProperty("attributes");
        attributes.GetProperty("auto_send").GetBoolean().Should().BeFalse();
        attributes.GetProperty("file_url_signature").GetString().Should().Be("$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV");
        var metaData = attributes.GetProperty("meta_data");
        metaData.GetProperty("invoice_number").GetString().Should().Be("Invoice 8051");
        metaData.GetProperty("invoice_date").GetString().Should().Be("2025-01-01");
        metaData.GetProperty("invoice_due_date").GetString().Should().Be("2025-01-31");
        metaData.GetProperty("recipient_identifier").GetString().Should().Be("41010560425610173");
        ebill.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("send", "ok"));
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
        await new EbillService(host.Client).CreateAsync(OrganisationId, content, CreateOptions, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        host.Api.Requests[0].Path.Should().Be("/file-upload");
        host.Files.Request.Method.Should().Be(HttpMethod.Put);
        host.Files.Request.Body.Should().Equal(Pdf);
        host.Files.Request.Header("Authorization").Should().BeNull();
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
        var act = () => new EbillService(host.Client).CreateAsync(
            OrganisationId,
            content,
            CreateOptions with { FileUrlSignature = "$2y$10$other" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("options");
        host.Api.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_one_ebill_is_addressed_GetAsync_gets_it_and_maps_the_abilities()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(SingleJson);

        // Act
        var ebill = await new EbillService(host.Client).GetAsync(OrganisationId, EbillId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(EbillPath);
        ebill.Meta!.Abilities.Should().BeEquivalentTo(new Dictionary<string, string> { ["send"] = "ok", ["cancel"] = "state" });
    }

    [Fact]
    public async Task When_an_ebill_was_created_without_auto_send_SendAsync_patches_send_without_a_body_and_maps_the_envelope()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(SingleJson);

        // Act
        var ebill = await new EbillService(host.Client).SendAsync(
            OrganisationId,
            EbillId,
            new() { IdempotencyKey = "3f0d9e21-4444-4000-8000-000000000004" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Path.Should().Be($"{EbillPath}/send");
        host.Api.Request.Body.Should().BeEmpty();
        host.Api.Request.Header("Content-Type").Should().BeNull();
        host.Api.Request.Header("Idempotency-Key").Should().Be("3f0d9e21-4444-4000-8000-000000000004");
        ebill.Id.Should().Be(EbillId);
        ebill.Attributes.InvoiceDate.Should().Be(new(2025, 10, 1));
    }

    [Fact]
    public async Task When_an_ebill_is_dropped_DeleteAsync_deletes_it_without_a_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty();

        // Act
        await new EbillService(host.Client).DeleteAsync(OrganisationId, EbillId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Delete);
        host.Api.Request.Path.Should().Be(EbillPath);
        host.Api.Request.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task When_an_ebill_is_still_cancellable_CancelAsync_patches_cancel_without_a_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await new EbillService(host.Client).CancelAsync(OrganisationId, EbillId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Path.Should().Be($"{EbillPath}/cancel");
        host.Api.Request.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task When_the_file_endpoint_redirects_GetFileLocationAsync_returns_the_location_and_DownloadFileAsync_streams_it_unauthenticated()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(new(HttpStatusCode.Found) { Headers = { Location = new("https://s3.example.com/bucket/5d2c8b70.pdf?signer=url") } });
        host.Files.Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) });

        // Act
        await using var file = await new EbillService(host.Client).DownloadFileAsync(OrganisationId, EbillId, TestContext.Current.CancellationToken);

        // Assert
        using var downloaded = new MemoryStream();
        await file.CopyToAsync(downloaded, TestContext.Current.CancellationToken);
        downloaded.ToArray().Should().Equal(Pdf);
        host.Api.Request.Path.Should().Be($"{EbillPath}/file");
        host.Files.Request.Url.Should().Be(new Uri("https://s3.example.com/bucket/5d2c8b70.pdf?signer=url"));
        host.Files.Request.Header("Authorization").Should().BeNull();
    }

    [Fact]
    public async Task When_the_events_of_an_ebill_are_listed_ListEventsAsync_gets_them_and_maps_the_parent()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(EventsJson);

        // Act
        var events = await new EbillService(host.Client).ListEventsAsync(OrganisationId, EbillId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Path.Should().Be($"{EbillPath}/events");
        var @event = events.Should().ContainSingle().Which;
        @event.Attributes.Code.Should().Be("approved");
        @event.Relationships!.Parent!.Data!.Type.Should().Be("ebills");
    }

    [Fact]
    public async Task When_an_event_image_is_requested_the_image_endpoint_is_addressed_by_event_id_and_streamed_unauthenticated()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(new(HttpStatusCode.Found) { Headers = { Location = new("https://s3.example.com/bucket/event.png?signer=url") } });
        host.Files.Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) });

        // Act
        await using var image = await new EbillService(host.Client).DownloadEventImageAsync(OrganisationId, EbillId, EventId, TestContext.Current.CancellationToken);

        // Assert
        using var downloaded = new MemoryStream();
        await image.CopyToAsync(downloaded, TestContext.Current.CancellationToken);
        downloaded.ToArray().Should().Equal(Pdf);
        host.Api.Request.Path.Should().Be($"{EbillPath}/events/{EventId}/image");
        host.Files.Request.Header("Authorization").Should().BeNull();
    }

    [Fact]
    public async Task When_the_ebill_cannot_be_sent_SendAsync_throws_with_the_parsed_errors()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueJson(
            HttpStatusCode.UnprocessableContent,
            """{ "errors": [ { "code": "17", "title": "Wrong state", "detail": "The ebill is not valid yet", "source": { "pointer": "/data/id" } } ] }"""
        );

        // Act
        var act = () => new EbillService(host.Client).SendAsync(OrganisationId, EbillId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        exception.Errors.Should().ContainSingle().Which.Title.Should().Be("Wrong state");
    }
}
