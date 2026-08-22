using System.Net;
using System.Text;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Tests;
using Pingen.Client.Webhooks;

namespace Pingen.Client.Tests.Webhooks;

public class WebhookServiceTests
{
    private static readonly Guid OrganisationId = Guid.Parse("6c3d1f0a-1111-4000-8000-000000000001");
    private static readonly Guid WebhookId = Guid.Parse("0a1b2c3d-9999-4000-8000-000000000009");
    private const string Prefix = "/organisations/6c3d1f0a-1111-4000-8000-000000000001/webhooks";
    private const string WebhookPath = $"{Prefix}/0a1b2c3d-9999-4000-8000-000000000009";

    private const string Abilities = """
                                     "meta": { "abilities": { "self": { "delete": "ok" } } },
                                     """;

    private static string WebhookJson(string eventCategory = "issues", string meta = "") =>
        $$"""
          {
            {{meta}}
            "id": "0a1b2c3d-9999-4000-8000-000000000009",
            "type": "webhooks",
            "attributes": {
              "event_category": "{{eventCategory}}",
              "url": "https://acme.example.com/hooks/pingen",
              "signing_key": "d09a095a0d1d2ae896f985c0fff1ad51"
            },
            "relationships": {
              "organisation": { "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" } }
            },
            "links": { "self": "https://api.pingen.com/organisations/6c3d1f0a/webhooks/0a1b2c3d" }
          }
          """;

    private static string ListJson =>
        $$"""
          {
            "data": [ {{WebhookJson("channel_subscriptions")}} ],
            "links": { "self": "https://api.pingen.com/organisations/6c3d1f0a/webhooks" },
            "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
          }
          """;

    [Fact]
    public async Task Given_an_endpoint_that_cannot_sort_When_the_webhooks_are_listed_ListAsync_drops_the_sort_and_keeps_the_rest()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(ListJson);

        // Act
        var webhooks = await new WebhookService(host.Client).ListAsync(
            OrganisationId,
            new() { PageNumber = 2, PageLimit = 50, Sort = "-created_at", Filter = PingenFilter.Where("event_category", "issues") },
            TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(Prefix);
        host.Api.Request.Query.Should().Be("?page[number]=2&page[limit]=50&filter=%7B%22event_category%22%3A%22issues%22%7D");
        var webhook = webhooks.Should().ContainSingle().Which;
        webhook.Id.Should().Be(WebhookId);
        webhook.Type.Should().Be("webhooks");
        webhook.Attributes.EventCategory.Should().Be(WebhookEventCategory.ChannelSubscriptions);
        webhook.Attributes.Url.Should().Be("https://acme.example.com/hooks/pingen");
        webhook.Attributes.SigningKey.Should().Be("d09a095a0d1d2ae896f985c0fff1ad51");
        webhook.Relationships!.Organisation!.Data!.Id.Should().Be("6c3d1f0a-1111-4000-8000-000000000001");
        webhook.Meta.Should().BeNull();
    }

    [Theory]
    [InlineData(WebhookEventCategory.Issues, "issues")]
    [InlineData(WebhookEventCategory.Sent, "sent")]
    [InlineData(WebhookEventCategory.Undeliverable, "undeliverable")]
    [InlineData(WebhookEventCategory.Delivered, "delivered")]
    [InlineData(WebhookEventCategory.ChannelSubscriptions, "channel_subscriptions")]
    public async Task When_a_subscription_is_created_CreateAsync_posts_the_envelope_with_the_wire_name_of_the_category(WebhookEventCategory category, string wireName)
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueJson(HttpStatusCode.Created, $$"""{ "data": {{WebhookJson(wireName, Abilities)}} }""");

        // Act
        var webhook = await new WebhookService(host.Client).CreateAsync(
            OrganisationId,
            new()
            {
                EventCategory = category,
                Url = "https://acme.example.com/hooks/pingen",
                SigningKey = "d09a095a0d1d2ae896f985c0fff1ad51",
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
        data.GetProperty("type").GetString().Should().Be("webhooks");
        data.TryGetProperty("id", out _).Should().BeFalse();
        data.TryGetProperty("relationships", out _).Should().BeFalse();
        var attributes = data.GetProperty("attributes");
        attributes.GetProperty("event_category").GetString().Should().Be(wireName);
        attributes.GetProperty("url").GetString().Should().Be("https://acme.example.com/hooks/pingen");
        attributes.GetProperty("signing_key").GetString().Should().Be("d09a095a0d1d2ae896f985c0fff1ad51");
        webhook.Attributes.EventCategory.Should().Be(category);
        webhook.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("delete", "ok"));
    }

    [Fact]
    public async Task When_one_webhook_is_addressed_GetAsync_gets_it_and_maps_the_abilities()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk($$"""{ "data": {{WebhookJson(meta: Abilities)}} }""");

        // Act
        var webhook = await new WebhookService(host.Client).GetAsync(OrganisationId, WebhookId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be(WebhookPath);
        webhook.Attributes.EventCategory.Should().Be(WebhookEventCategory.Issues);
        webhook.Meta!.Abilities.Should().BeEquivalentTo(new Dictionary<string, string> { ["delete"] = "ok" });
    }

    [Fact]
    public async Task When_a_subscription_is_cancelled_DeleteAsync_deletes_it_without_a_body()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty();

        // Act
        await new WebhookService(host.Client).DeleteAsync(OrganisationId, WebhookId, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Delete);
        host.Api.Request.Path.Should().Be(WebhookPath);
        host.Api.Request.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task When_the_signing_key_is_too_short_CreateAsync_throws_with_the_parsed_errors_and_the_request_id()
    {
        // Arrange
        using var host = new PingenTestHost();
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableContent)
        {
            Content = new StringContent(
                """{ "errors": [ { "code": "22", "title": "Validation failed", "detail": "signing_key must be at least 20 characters", "source": { "pointer": "/data/attributes/signing_key" } } ] }""",
                Encoding.UTF8,
                PingenClient.JsonApiMediaType
            ),
        };
        response.Headers.Add("X-Request-Id", "0HN7A2K3L4M5N");
        host.Api.Enqueue(response);

        // Act
        var act = () => new WebhookService(host.Client).CreateAsync(
            OrganisationId,
            new() { EventCategory = WebhookEventCategory.Sent, Url = "https://acme.example.com/hooks/pingen", SigningKey = "short" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.UnprocessableContent);
        exception.RequestId.Should().Be("0HN7A2K3L4M5N");
        exception.Errors.Should().ContainSingle().Which.Source!.Pointer.Should().Be("/data/attributes/signing_key");
    }
}
