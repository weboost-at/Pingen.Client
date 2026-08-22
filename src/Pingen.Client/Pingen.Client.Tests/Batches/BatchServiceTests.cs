using System.Net;
using System.Text;
using FluentAssertions;
using Pingen.Client.Batches;
using Pingen.Client.Batches.ValueTypes;
using Pingen.Client.Common;
using Pingen.Client.Deliveries.ValueTypes;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests.Batches;

public class BatchServiceTests
{
    private static readonly Guid Organisation = Guid.Parse("6c3d1f0a-1111-4000-8000-000000000001");

    private static readonly Guid BatchId = Guid.Parse("2a4c9e77-2222-4000-8000-000000000002");

    private static readonly Guid PresetId = Guid.Parse("7f0b2c55-4444-4000-8000-000000000004");

    private const string BatchesPath = "/organisations/6c3d1f0a-1111-4000-8000-000000000001/batches";

    private const string BatchPath = $"{BatchesPath}/2a4c9e77-2222-4000-8000-000000000002";

    private static readonly byte[] Archive = "PK Zürich"u8.ToArray();

    [Fact]
    public async Task When_a_page_is_requested_ListAsync_gets_the_batches_path_with_the_query_and_maps_the_page()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(PageJson(1, 2));
        var options = new PingenListOptions
        {
            PageNumber = 1,
            PageLimit = 1,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "processing"),
            Fields = new Dictionary<string, string> { ["batches"] = "name,status" },
        };

        // Act
        var page = await new BatchService(host.Client).ListAsync(Organisation, options, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(BatchesPath);
        Uri.UnescapeDataString(host.Api.Request.Query).Should()
            .Be("""?page[number]=1&page[limit]=1&sort=-created_at&filter={"status":"processing"}&fields[batches]=name,status""");
        var batch = page.Should().ContainSingle().Which;
        batch.Attributes.Icon.Should().Be("wave-hand");
        batch.Attributes.ChannelType.Should().Be("post");
        batch.Attributes.SubmittedAt.Should().BeNull();
        batch.Relationships!.Events!.Count.Should().Be(3);
        batch.Meta.Should().BeNull();
        page.Meta!.Total.Should().Be(2);
        page.Links!.Next.Should().Be($"https://api.pingen.com{BatchesPath}?page[number]=2");
    }

    [Fact]
    public async Task Given_a_collection_spanning_two_pages_When_it_is_enumerated_ListAutoPagingAsync_yields_every_batch_and_stops_at_the_last_page()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(PageJson(1, 2)).EnqueueOk(PageJson(2, 2));

        // Act
        var batches = await new BatchService(host.Client)
            .ListAutoPagingAsync(Organisation, new() { PageLimit = 1 }, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        batches.Select(batch => batch.Attributes.LetterCount).Should().Equal(1, 2);
        host.Api.Requests.Should().HaveCount(2);
        Uri.UnescapeDataString(host.Api.Requests[0].Query).Should().Be("?page[limit]=1");
        Uri.UnescapeDataString(host.Api.Requests[1].Query).Should().Be("?page[number]=2&page[limit]=1");
    }

    [Fact]
    public async Task When_the_archive_was_uploaded_already_CreateAsync_posts_the_batches_envelope_with_the_grouping_options_and_the_preset()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueJson(HttpStatusCode.Created, SingleJson());

        // Act
        var batch = await new BatchService(host.Client).CreateAsync(
            Organisation,
            new()
            {
                Name = "Rechnungslauf Zürich",
                Icon = BatchIcon.WaveHand,
                FileOriginalName = "lörem.zip",
                FileUrl = "https://s3.example.com/bucket/934b6a01.zip?signer=url",
                FileUrlSignature = "$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV",
                GroupingType = BatchGroupingType.Merge,
                SplitType = BatchSplitType.QrInvoice,
                ChannelType = BatchChannelType.Post,
                AddressPosition = AddressPosition.Right,
                SplitSize = 2,
                SplitSeparator = "Trennblatt",
                SplitPosition = BatchSplitPosition.FirstPage,
                PresetId = PresetId,
            },
            new PingenRequestOptions { IdempotencyKey = "b3f1a2c4-5555-4000-8000-000000000005" },
            TestContext.Current.CancellationToken
        );

        // Assert
        var request = host.Api.Request;
        request.Method.Should().Be(HttpMethod.Post);
        request.Path.Should().Be(BatchesPath);
        request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        request.Header("Idempotency-Key").Should().Be("b3f1a2c4-5555-4000-8000-000000000005");
        var data = request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("batches");
        data.TryGetProperty("id", out _).Should().BeFalse();
        var attributes = data.GetProperty("attributes");
        attributes.GetProperty("name").GetString().Should().Be("Rechnungslauf Zürich");
        attributes.GetProperty("icon").GetString().Should().Be("wave-hand");
        attributes.GetProperty("grouping_type").GetString().Should().Be("merge");
        attributes.GetProperty("grouping_options_split_type").GetString().Should().Be("qr_invoice");
        attributes.GetProperty("grouping_options_split_size").GetInt32().Should().Be(2);
        attributes.GetProperty("grouping_options_split_position").GetString().Should().Be("first_page");
        attributes.GetProperty("channel_type").GetString().Should().Be("post");
        attributes.GetProperty("address_position").GetString().Should().Be("right");
        data.GetProperty("relationships").GetProperty("preset").GetProperty("data").GetProperty("id").GetString().Should().Be(PresetId.ToString());
        batch.Id.Should().Be(BatchId);
    }

    [Fact]
    public async Task When_a_stream_is_given_CreateAsync_requests_an_upload_target_puts_the_raw_bytes_and_posts_the_copied_url()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(UploadJson).EnqueueJson(HttpStatusCode.Created, SingleJson());
        host.Files.EnqueueEmpty(HttpStatusCode.OK);
        using var content = new MemoryStream(Archive);

        // Act
        var batch = await new BatchService(host.Client).CreateAsync(
            Organisation,
            content,
            CreateOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Requests[0].Path.Should().Be("/file-upload");
        host.Files.Request.Method.Should().Be(HttpMethod.Put);
        host.Files.Request.Body.Should().Equal(Archive);
        host.Files.Request.Header("Authorization").Should().BeNull();
        var attributes = host.Api.Requests[1].Json.GetProperty("data").GetProperty("attributes");
        host.Api.Requests[1].Path.Should().Be(BatchesPath);
        attributes.GetProperty("file_url").GetString().Should().Be("https://s3.example.com/bucket/934b6a01.zip?signer=url");
        attributes.GetProperty("file_url_signature").GetString().Should().Be("$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV");
        batch.Id.Should().Be(BatchId);
    }

    [Fact]
    public async Task When_the_options_already_carry_an_upload_target_the_stream_overload_of_CreateAsync_refuses_the_call()
    {
        // Arrange
        using var host = new PingenTestHost();
        using var content = new MemoryStream(Archive);

        // Act
        var act = () => new BatchService(host.Client).CreateAsync(
            Organisation,
            content,
            CreateOptions with { FileUrl = "https://s3.example.com/bucket/934b6a01.zip" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("options");
        host.Api.Requests.Should().BeEmpty();
        host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_single_batch_is_requested_GetAsync_gets_its_path_and_maps_the_abilities()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(SingleJson());

        // Act
        var batch = await new BatchService(host.Client).GetAsync(Organisation, BatchId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(BatchPath);
        host.Api.Request.Query.Should().BeEmpty();
        host.Api.Request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");
        batch.Meta!.Abilities!["cancel"].Should().Be("ok");
        batch.Attributes.PriceValue.Should().Be(1.25m);
    }

    [Fact]
    public async Task When_a_batch_is_renamed_EditAsync_patches_its_path_with_the_id_repeated_in_the_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await new BatchService(host.Client).EditAsync(
            Organisation,
            BatchId,
            new() { Name = "Rechnungslauf Zürich", Icon = BatchIcon.PercentTag },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Path.Should().Be(BatchPath);
        host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        var data = host.Api.Request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("batches");
        data.GetProperty("id").GetString().Should().Be(BatchId.ToString());
        data.GetProperty("attributes").GetProperty("name").GetString().Should().Be("Rechnungslauf Zürich");
        data.GetProperty("attributes").GetProperty("icon").GetString().Should().Be("percent-tag");
    }

    [Fact]
    public async Task When_a_batch_is_deleted_DeleteAsync_sends_the_required_body_along_with_the_delete()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty();

        // Act
        await new BatchService(host.Client).DeleteAsync(
            Organisation,
            BatchId,
            new() { WithLetters = true, WithDeliverables = false },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Delete);
        host.Api.Request.Path.Should().Be(BatchPath);
        host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        var data = host.Api.Request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be("batches");
        data.GetProperty("id").GetString().Should().Be(BatchId.ToString());
        data.GetProperty("attributes").GetProperty("with_letters").GetBoolean().Should().BeTrue();
        data.GetProperty("attributes").GetProperty("with_deliverables").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task When_only_the_batch_is_deleted_DeleteAsync_omits_the_deliverables_flag()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty();

        // Act
        await new BatchService(host.Client).DeleteAsync(
            Organisation,
            BatchId,
            new() { WithLetters = false },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        var attributes = host.Api.Request.Json.GetProperty("data").GetProperty("attributes");
        attributes.GetProperty("with_letters").GetBoolean().Should().BeFalse();
        attributes.TryGetProperty("with_deliverables", out _).Should().BeFalse();
    }

    [Fact]
    public async Task When_a_batch_is_cancelled_CancelAsync_patches_the_cancel_path_without_a_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await new BatchService(host.Client).CancelAsync(
            Organisation,
            BatchId,
            new PingenRequestOptions { IdempotencyKey = "b3f1a2c4-5555-4000-8000-000000000005" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Path.Should().Be($"{BatchPath}/cancel");
        host.Api.Request.Body.Should().BeEmpty();
        host.Api.Request.Header("Idempotency-Key").Should().Be("b3f1a2c4-5555-4000-8000-000000000005");
    }

    [Theory]
    [InlineData("post", "batches_channel_post_send", "fast")]
    [InlineData("email", "batches_channel_email_send", "electronic_email")]
    [InlineData("ebill", "batches_channel_ebill_send", "electronic_ebill")]
    public async Task When_a_batch_is_sent_SendAsync_patches_the_send_path_with_the_type_of_the_channel_and_the_id_repeated(string channel, string type, string product)
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(SingleJson());
        var options = channel switch
        {
            "post" => BatchSendOptions.Post(DeliveryProduct.Fast, PrintMode.Simplex, PrintSpectrum.Color),
            "email" => BatchSendOptions.Email(),
            _ => BatchSendOptions.Ebill(),
        };

        // Act
        var batch = await new BatchService(host.Client).SendAsync(Organisation, BatchId, options, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Path.Should().Be($"{BatchPath}/send");
        var data = host.Api.Request.Json.GetProperty("data");
        data.GetProperty("type").GetString().Should().Be(type);
        data.GetProperty("id").GetString().Should().Be(BatchId.ToString());
        var attributes = data.GetProperty("attributes");
        attributes.GetProperty("delivery_product").GetString().Should().Be(product);
        attributes.TryGetProperty("print_mode", out var printMode).Should().Be(channel is "post");
        if (channel is "post") printMode.GetString().Should().Be("simplex");
        batch.Attributes.Status.Should().Be("processing");
    }

    [Fact]
    public async Task When_the_events_of_a_batch_are_listed_ListEventsAsync_gets_the_events_path_with_the_language()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(EventsJson);

        // Act
        var events = await new BatchService(host.Client).ListEventsAsync(
            Organisation,
            BatchId,
            new() { Language = "de-DE", Sort = "-emitted_at" },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Path.Should().Be($"{BatchPath}/events");
        Uri.UnescapeDataString(host.Api.Request.Query).Should().Be("?sort=-emitted_at&language=de-DE");
        var @event = events.Should().ContainSingle().Which;
        @event.Type.Should().Be("batches_events");
        @event.Attributes.Code.Should().Be("processing");
        @event.Attributes.Data.Should().Equal("split");
        @event.Attributes.EmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        @event.Relationships!.Batch!.Data!.Type.Should().Be("batches");
    }

    [Fact]
    public async Task When_the_statistics_are_requested_GetStatisticsAsync_gets_the_statistics_path_and_maps_the_distributions()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(StatisticsJson);

        // Act
        var statistics = await new BatchService(host.Client).GetStatisticsAsync(Organisation, BatchId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be($"{BatchPath}/statistics");
        statistics.Type.Should().Be("batch_details_statistics");
        statistics.Attributes.LetterValidating.Should().Be(2);
        statistics.Attributes.LetterGroups.Select(group => group.Name).Should().Equal("valid", "not_available");
        statistics.Attributes.LetterCountries.Should().ContainSingle().Which.Country.Should().Be("CH");
        statistics.Attributes.LetterRegions.Should().ContainSingle().Which.Count.Should().Be(3);
    }

    [Fact]
    public async Task When_the_api_rejects_the_batch_CreateAsync_throws_with_the_parsed_errors_and_the_request_id()
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
        var act = () => new BatchService(host.Client).CreateAsync(Organisation, CreateOptions, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        exception.RequestId.Should().Be("3d1f0a6c-6666-4000-8000-000000000006");
        exception.Errors.Should().ContainSingle().Which.Source!.Pointer.Should().Be("/data/attributes/name");
        exception.Message.Should().Contain("422");
    }

    private static BatchCreateOptions CreateOptions =>
        new()
        {
            Name = "Rechnungslauf Zürich",
            Icon = BatchIcon.WaveHand,
            FileOriginalName = "lörem.zip",
            GroupingType = BatchGroupingType.Zip,
            SplitType = BatchSplitType.File,
        };

    private static string SingleJson() => $$"""{ "data": {{BatchJson(2, withMeta: true)}} }""";

    private static string PageJson(int number, int lastPage) =>
        $$"""
          {
            "data": [ {{BatchJson(number, withMeta: false)}} ],
            "links": {
              "first": "https://api.pingen.com{{BatchesPath}}?page[number]=1",
              "last": "https://api.pingen.com{{BatchesPath}}?page[number]={{lastPage}}",
              "next": "https://api.pingen.com{{BatchesPath}}?page[number]=2",
              "self": "https://api.pingen.com{{BatchesPath}}?page[number]={{number}}"
            },
            "meta": { "current_page": {{number}}, "last_page": {{lastPage}}, "per_page": 1, "from": {{number}}, "to": {{number}}, "total": 2 }
          }
          """;

    private static string BatchJson(int letters, bool withMeta) =>
        $$"""
          {
            "id": "2a4c9e77-2222-4000-8000-000000000002",
            "type": "batches",
            "attributes": {
              "name": "Rechnungslauf Zürich",
              "channel_type": "post",
              "icon": "wave-hand",
              "status": "processing",
              "file_original_name": "lörem.zip",
              "letter_count": {{letters}},
              "deliverable_count": {{letters}},
              "address_position": "left",
              "print_mode": "simplex",
              "print_spectrum": "color",
              "price_currency": "CHF",
              "price_value": 1.25,
              "source": "api",
              "submitted_at": "",
              "created_at": "2020-11-19T09:42:48+0100",
              "updated_at": "2020-11-20T10:00:00+0100"
            },
            "relationships": {
              "organisation": { "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" } },
              "events": { "links": { "related": { "href": "https://api.pingen.com/events", "meta": { "count": 3 } } } }
            },
            "links": { "self": "https://api.pingen.com{{BatchPath}}" }{{(withMeta ? AbilitiesJson : string.Empty)}}
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
                                            "url": "https://s3.example.com/bucket/934b6a01.zip?signer=url",
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
                                            "type": "batches_events",
                                            "attributes": {
                                              "code": "processing",
                                              "name": "Wird verarbeitet",
                                              "producer": "Pingen",
                                              "location": "8051 Zürich, CH",
                                              "data": ["split"],
                                              "emitted_at": "2021-11-19T09:42:48+0100",
                                              "created_at": "2021-11-19T09:42:48+0100",
                                              "updated_at": "2021-11-19T09:42:48+0100"
                                            },
                                            "relationships": {
                                              "batch": { "data": { "id": "2a4c9e77-2222-4000-8000-000000000002", "type": "batches" } }
                                            }
                                          }
                                        ],
                                        "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
                                      }
                                      """;

    private const string StatisticsJson = """
                                          {
                                            "data": {
                                              "id": "2a4c9e77-2222-4000-8000-000000000002",
                                              "type": "batch_details_statistics",
                                              "attributes": {
                                                "letter_validating": 2,
                                                "letter_groups": [{ "name": "valid", "count": 3 }, { "name": "not_available", "count": 1 }],
                                                "letter_countries": [{ "country": "CH", "count": 3 }],
                                                "letter_regions": [{ "country": "CH", "count": 3 }]
                                              },
                                              "links": { "self": "https://api.pingen.com/statistics" }
                                            }
                                          }
                                          """;

    private const string ErrorsJson = """
                                      {
                                        "errors": [
                                          {
                                            "code": "22",
                                            "title": "The given data was invalid.",
                                            "detail": "The name must be at least 5 characters.",
                                            "source": { "pointer": "/data/attributes/name", "parameter": null }
                                          }
                                        ]
                                      }
                                      """;
}
