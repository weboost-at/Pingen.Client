using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Webhooks.Support;
using Pingen.Client.Webhooks;

namespace Pingen.Client.Tests.Webhooks;

public class WebhookServiceTests : WebhookTestBase
{
    [Fact]
    public async Task Given_A_Sort_ListAsync_Drops_It_And_Keeps_The_Rest_Of_The_Query()
    {
        // Arrange
        Host.Api.EnqueueOk(WebhookPage());

        var options = new PingenListOptions
        {
            PageNumber = 2,
            PageLimit = 50,
            Sort = "-created_at",
            Filter = PingenFilter.Where(WebhookField.EventCategory, "issues")
        };

        // Act
        var webhooks = await Webhooks.ListAsync(Organisation.Id, options, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, WebhooksPath);
        Host.Api.Request.Assert.Query("""?page[number]=2&page[limit]=50&filter={"event_category":"issues"}""");

        webhooks.Should().ContainSingle();
        webhooks[0].Meta.Should().BeNull();
        webhooks[0].Id.Should().Be(Webhook.Id);
        webhooks[0].Type.Should().Be("webhooks");
        webhooks[0].Attributes.Url.Should().Be(Webhook.Url);
        webhooks[0].Attributes.SigningKey.Should().Be(Webhook.SigningKey);
        webhooks[0].Attributes.EventCategory.Should().Be(WebhookEventCategory.ChannelSubscriptions);
        webhooks[0].Relationships!.Organisation!.Data!.Id.Should().Be(Organisation.Id.ToString());
    }

    [Fact]
    public async Task CreateAsync_Posts_The_Envelope_With_The_Idempotency_Key()
    {
        // Arrange
        Host.Api.EnqueueJson(HttpStatusCode.Created, WebhookDocument("issues"));

        var options = new WebhookCreateOptions
        {
            EventCategory = WebhookEventCategory.Issues,
            Url = Webhook.Url,
            SigningKey = Webhook.SigningKey
        };

        // Act
        var webhook = await Webhooks.CreateAsync(Organisation.Id, options, new() { IdempotencyKey = Request.IdempotencyKey }, Ct);

        // Assert
        webhook.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("delete", "ok"));

        Host.Api.Request.Assert.Sent(HttpMethod.Post, WebhooksPath);
        Host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);

        Host.Api.Request.Data.Assert.Lacks("id");
        Host.Api.Request.Data.Assert.Has("type", "webhooks");
        Host.Api.Request.Data.Assert.Lacks("relationships");
        Host.Api.Request.Attributes.Assert.Has("url", Webhook.Url);
        Host.Api.Request.Attributes.Assert.Has("signing_key", Webhook.SigningKey);
    }

    [Theory]
    [InlineData(WebhookEventCategory.Issues, "issues")]
    [InlineData(WebhookEventCategory.Sent, "sent")]
    [InlineData(WebhookEventCategory.Undeliverable, "undeliverable")]
    [InlineData(WebhookEventCategory.Delivered, "delivered")]
    [InlineData(WebhookEventCategory.ChannelSubscriptions, "channel_subscriptions")]
    public async Task Given_A_Category_CreateAsync_Writes_Its_Wire_Name(WebhookEventCategory category, string wire)
    {
        // Arrange
        Host.Api.EnqueueJson(HttpStatusCode.Created, WebhookDocument(wire));

        var options = new WebhookCreateOptions
        {
            EventCategory = category,
            Url = Webhook.Url,
            SigningKey = Webhook.SigningKey
        };

        // Act
        var webhook = await Webhooks.CreateAsync(Organisation.Id, options, cancellationToken: Ct);

        // Assert
        webhook.Attributes.EventCategory.Should().Be(category);

        Host.Api.Request.Attributes.Assert.Has("event_category", wire);
    }

    [Fact]
    public async Task GetAsync_Gets_The_Webhook_And_Maps_The_Abilities()
    {
        // Arrange
        Host.Api.EnqueueOk(WebhookDocument("issues"));

        // Act
        var webhook = await Webhooks.GetAsync(Organisation.Id, Webhook.Id, Ct);

        // Assert
        webhook.Attributes.EventCategory.Should().Be(WebhookEventCategory.Issues);
        webhook.Meta!.Abilities.Should().BeEquivalentTo(new Dictionary<string, string> { ["delete"] = "ok" });

        Host.Api.Request.Assert.Sent(HttpMethod.Get, WebhookPath);
    }

    [Fact]
    public async Task DeleteAsync_Deletes_The_Webhook_Without_A_Body()
    {
        // Arrange
        Host.Api.EnqueueEmpty();

        // Act
        await Webhooks.DeleteAsync(Organisation.Id, Webhook.Id, Ct);

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Assert.Sent(HttpMethod.Delete, WebhookPath);
    }

    [Fact]
    public async Task Given_A_Rejection_CreateAsync_Throws_With_The_Errors_And_The_Request_Id()
    {
        // Arrange
        Host.Api.Enqueue(Rejection(HttpStatusCode.UnprocessableContent));

        var options = new WebhookCreateOptions
        {
            EventCategory = WebhookEventCategory.Sent,
            Url = Webhook.Url,
            SigningKey = Webhook.SigningKey
        };

        // Act
        var act = () => Webhooks.CreateAsync(Organisation.Id, options, cancellationToken: Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.UnprocessableContent);
        exception.RequestId.Should().Be(Request.Id);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Source!.Pointer.Should().Be(Error.Pointer);
    }
}
