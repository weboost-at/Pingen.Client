using System.Text;
using FluentAssertions;
using Pingen.Client.Tests.Webhooks.Support;
using Pingen.Client.Webhooks;
using Pingen.Client.Webhooks.Payloads;

namespace Pingen.Client.Tests.Webhooks;

public class PingenWebhookTests : WebhookTestBase
{
    [Theory]
    [InlineData("webhook_issues", typeof(WebhookIssueEvent))]
    [InlineData("webhook_sent", typeof(WebhookSentEvent))]
    [InlineData("webhook_delivered", typeof(WebhookDeliveredEvent))]
    [InlineData("webhook_undeliverable", typeof(WebhookUndeliverableEvent))]
    [InlineData("webhook_channel_subscriptions", typeof(WebhookChannelSubscriptionEvent))]
    public void Given_A_Signed_Payload_ConstructEvent_Returns_The_Event_Its_Type_Names(string type, Type expected)
    {
        // Arrange
        var payload = Payload(type);

        // Act
        var @event = PingenWebhook.ConstructEvent(payload, Sign(payload), Webhook.SigningKey);

        // Assert
        @event.Should().BeOfType(expected);
        @event.Type.Should().Be(type);
        @event.Id.Should().Be(Webhook.EventId);
        @event.Url.Should().Be(Webhook.Url);
        @event.CreatedAt.Should().Be(new DateTimeOffset(2020, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        @event.Organisation!.Data!.Id.Should().Be(Organisation.Id.ToString());
    }

    [Fact]
    public void Given_An_Issues_Payload_ConstructEvent_Maps_The_Reason_And_The_Deliverable()
    {
        // Arrange
        var payload = IssuesPayload();

        // Act
        var @event = PingenWebhook.ConstructEvent(payload, Sign(payload), Webhook.SigningKey);

        // Assert
        var issue = @event.Assert.Is<WebhookIssueEvent>();
        issue.Reason.Should().Be(Webhook.IssueReason);
        issue.Event!.Data!.Type.Should().Be("letters_events");
        issue.Deliverable!.Data!.Type.Should().Be("letters");
        issue.Deliverable.Data.Id.Should().Be(Webhook.DeliverableId.ToString());
    }

    [Fact]
    public void Given_An_Undeliverable_Payload_ConstructEvent_Maps_The_Reason_And_The_Corrected_Address()
    {
        // Arrange
        var payload = UndeliverablePayload();

        // Act
        var @event = PingenWebhook.ConstructEvent(payload, Sign(payload), Webhook.SigningKey);

        // Assert
        var undeliverable = @event.Assert.Is<WebhookUndeliverableEvent>();
        undeliverable.Reason.Should().Be(Webhook.UndeliverableReason);
        undeliverable.CorrectedAddress.Should().Be(Webhook.CorrectedAddress);
        undeliverable.Event!.Data!.Id.Should().Be(Webhook.DeliveryEventId.ToString());
    }

    [Fact]
    public void Given_A_Channel_Subscription_ConstructEvent_Maps_The_Subscription_And_Its_Channel()
    {
        // Arrange
        var payload = ChannelSubscriptionPayload();

        // Act
        var @event = PingenWebhook.ConstructEvent(payload, Sign(payload), Webhook.SigningKey);

        // Assert
        var subscription = @event.Assert.Is<WebhookChannelSubscriptionEvent>();
        subscription.Name.Should().Be(Webhook.Name);
        subscription.Email.Should().Be(Webhook.Email);
        subscription.Status.Should().Be("requested");
        subscription.ApprovedAt.Should().BeNull();
        subscription.Identifier.Should().Be(Webhook.Identifier);
        subscription.Address.Should().Be("Hauptstrasse 24\n4001 Zürich");
        subscription.ChannelEbill!.Data!.Type.Should().Be("channel_ebills");
        subscription.ChannelEbill.Data.Id.Should().Be(Webhook.ChannelEbillId.ToString());
    }

    [Fact]
    public void ParseEvent_Parses_The_Payload_Unverified()
    {
        // Act
        var @event = PingenWebhook.ParseEvent(SentPayload());

        // Assert
        var sent = @event.Assert.Is<WebhookSentEvent>();
        sent.Deliverable!.Data!.Type.Should().Be("letters");
    }

    [Fact]
    public void Given_A_Unicode_Payload_VerifySignature_Accepts_Its_Exact_Bytes_Only()
    {
        // Arrange
        var payload = UndeliverablePayload();
        var signature = Sign(payload);

        // Act
        var accepted = PingenWebhook.VerifySignature(payload, signature, Webhook.SigningKey);
        var reformatted = PingenWebhook.VerifySignature(payload.Replace("\n", ""), signature, Webhook.SigningKey);
        var decomposed = PingenWebhook.VerifySignature(payload.Normalize(NormalizationForm.FormD), signature, Webhook.SigningKey);

        // Assert
        accepted.Should().BeTrue();
        decomposed.Should().BeFalse();
        reformatted.Should().BeFalse();
    }

    [Fact]
    public void Given_Uppercase_Hex_VerifySignature_Accepts_It()
    {
        // Arrange
        var payload = SentPayload();

        // Act
        var accepted = PingenWebhook.VerifySignature(payload, Sign(payload).ToUpperInvariant(), Webhook.SigningKey);

        // Assert
        accepted.Should().BeTrue();
    }

    [Fact]
    public void Given_A_Signature_Of_Another_Key_VerifySignature_Rejects_It()
    {
        // Arrange
        var payload = SentPayload();

        // Act
        var accepted = PingenWebhook.VerifySignature(payload, Sign(payload, Webhook.OtherSigningKey), Webhook.SigningKey);

        // Assert
        accepted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("Zürich 🇨🇭")]
    [InlineData("not-a-signature")]
    [InlineData("d09a095a0d1d2ae896f985c0fff1ad51")]
    [InlineData("000000000000000000000000000000000000000000000000000000000000000000")]
    public void Given_A_Header_That_Is_No_Sha256_Hex_Digest_VerifySignature_Rejects_It(string signatureHeader) =>
        PingenWebhook.VerifySignature(SentPayload(), signatureHeader, Webhook.SigningKey).Should().BeFalse();

    [Fact]
    public void Given_A_Header_Missing_Its_Last_Zero_Byte_VerifySignature_Rejects_It()
    {
        // Arrange
        var signature = Sign(WebhookTestData.ZeroEndingPayload, WebhookTestData.ZeroEndingKey);

        // Act
        var full = PingenWebhook.VerifySignature(WebhookTestData.ZeroEndingPayload, signature, WebhookTestData.ZeroEndingKey);
        var truncated = PingenWebhook.VerifySignature(WebhookTestData.ZeroEndingPayload, signature[..^2], WebhookTestData.ZeroEndingKey);

        // Assert
        signature.Should().EndWith("00");

        full.Should().BeTrue();
        truncated.Should().BeFalse();
    }

    [Fact]
    public void Given_A_Signature_Of_Another_Key_ConstructEvent_Rejects_The_Payload()
    {
        // Arrange
        var payload = SentPayload();

        // Act
        var act = () => PingenWebhook.ConstructEvent(payload, Sign(payload, Webhook.OtherSigningKey), Webhook.SigningKey);

        // Assert
        var error = act.Assert.ThrowsRejection();
        error.Detail.Should().Be(
            "The signature does not match the payload - it was not signed with this webhook's key or the body was altered on the way."
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("Zürich 🇨🇭")]
    [InlineData("not json at all")]
    [InlineData("""{"meta":{}}""")]
    [InlineData("""{"data":[]}""")]
    [InlineData("""{"data":{"type":42}}""")]
    [InlineData("""{"data":{"attributes":{}}}""")]
    [InlineData("""{"data":{"type":"webhook_sent","attributes":{}}}""")]
    [InlineData("""{"data":{"type":"webhook_sent","attributes":{"url":42}}}""")]
    [InlineData("""{"data":{"id":"0a1b2c3d-9999-4000-8000-000000000009","type":"webhook_sent"}}""")]
    [InlineData("""{"data":{"type":"webhook_sent","attributes":{"url":"https://büro.example.ch","created_at":"2020-11-19T09:42:48+0100"}}}""")]
    [InlineData("""{"data":{"id":"not-a-guid","type":"webhook_sent","attributes":{"url":"https://büro.example.ch","created_at":"2020-11-19T09:42:48+0100"}}}""")]
    [InlineData("""{"data":{"id":"0a1b2c3d-9999-4000-8000-000000000009","type":"webhook_sent","attributes":{"url":"https://büro.example.ch","created_at":"gestern in Zürich"}}}""")]
    public void Given_A_Malformed_Payload_ParseEvent_Rejects_It(string payload)
    {
        // Act
        var act = () => PingenWebhook.ParseEvent(payload);

        // Assert
        var error = act.Assert.ThrowsRejection();
        error.Detail.Should().StartWith("The payload is not a webhook event document - ");
    }

    [Theory]
    [InlineData("webhook_issues")]
    [InlineData("webhook_sent")]
    [InlineData("webhook_delivered")]
    [InlineData("webhook_undeliverable")]
    [InlineData("webhook_channel_subscriptions")]
    public void Given_Null_Attributes_ParseEvent_Rejects_The_Payload_For_Carrying_None(string type)
    {
        // Arrange
        var payload = $$"""{ "data": { "id": "{{Webhook.EventId}}", "type": "{{type}}", "attributes": null } }""";

        // Act
        var act = () => PingenWebhook.ParseEvent(payload);

        // Assert
        var error = act.Assert.ThrowsRejection();
        error.Detail.Should().Be("The payload carries no attributes.");
    }

    [Fact]
    public void Given_A_Signed_Malformed_Payload_ConstructEvent_Rejects_It_Instead_Of_Throwing_A_Json_Error()
    {
        // Arrange
        var payload = """{"data":{"type":"webhook_sent","attributes":{}}}""";

        // Act
        var act = () => PingenWebhook.ConstructEvent(payload, Sign(payload), Webhook.SigningKey);

        // Assert
        act.Assert.ThrowsRejection();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("webhooks")]
    [InlineData("WEBHOOK_SENT")]
    [InlineData("webhook_returned")]
    [InlineData("webhook_zurückgesendet_📮")]
    public void Given_An_Unknown_Type_ConstructEvent_Throws_Naming_It(string? type)
    {
        // Arrange
        var payload = Payload(type);

        // Act
        var act = () => PingenWebhook.ConstructEvent(payload, Sign(payload), Webhook.SigningKey);

        // Assert
        var error = act.Assert.ThrowsRejection();
        error.Detail.Should().Be($"'{type}' is not a webhook event type this version of the SDK knows.");
    }
}
