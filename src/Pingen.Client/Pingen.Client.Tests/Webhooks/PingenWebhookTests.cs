using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Webhooks;
using Pingen.Client.Webhooks.Payloads;

namespace Pingen.Client.Tests.Webhooks;

public class PingenWebhookTests
{
    private const string SigningKey = "d09a095a0d1d2ae896f985c0fff1ad51";
    private static readonly Guid EventId = Guid.Parse("0a1b2c3d-9999-4000-8000-000000000009");
    private static readonly DateTimeOffset CreatedAt = new(2020, 11, 19, 9, 42, 48, TimeSpan.FromHours(1));

    private const string BaseAttributes = """
                                          "url": "https://acme.example.com/hooks/pingen",
                                          "created_at": "2020-11-19T09:42:48+0100"
                                          """;

    private const string DeliverableRelationships = """
                                                    "organisation": { "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" } },
                                                    "deliverable": { "data": { "id": "2b4c6d8e-2222-4000-8000-000000000002", "type": "letters" } },
                                                    "event": { "data": { "id": "7b1f9c22-3333-4000-8000-000000000003", "type": "letters_events" } }
                                                    """;

    private const string ChannelRelationships = """
                                                "organisation": { "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" } },
                                                "channel_ebill": { "data": { "id": "5d2c8b70-7777-4000-8000-000000000007", "type": "channel_ebills" } }
                                                """;

    private static string Payload(string type, string attributes, string relationships = DeliverableRelationships) =>
        $$"""
          {
            "data": {
              "id": "0a1b2c3d-9999-4000-8000-000000000009",
              "type": "{{type}}",
              "attributes": { {{attributes}} },
              "relationships": { {{relationships}} },
              "links": { "self": "https://api.pingen.com/webhooks/0a1b2c3d" }
            }
          }
          """;

    private static string SentPayload => Payload("webhook_sent", BaseAttributes);

    private static string DeliveredPayload => Payload("webhook_delivered", BaseAttributes);

    private static string IssuesPayload => Payload(
        "webhook_issues",
        $$"""
          {{BaseAttributes}},
          "reason": "Content failed inspection"
          """
    );

    private static string UndeliverablePayload => Payload(
        "webhook_undeliverable",
        $$"""
          {{BaseAttributes}},
          "reason": "Recipient could not be determined at the specified address.",
          "corrected_address": { "name": "Alex Meier 🇨🇭", "street": "Example street", "number": "50A", "zip": "8051", "city": "Zürich" }
          """
    );

    private static string ChannelSubscriptionPayload => Payload(
        "webhook_channel_subscriptions",
        $$"""
          {{BaseAttributes}},
          "identifier": "41020580424610132",
          "email": "jürgen@example.com",
          "name": "Jürgen Zürcher",
          "address": "Hauptstrasse 24\n4001 Zürich",
          "status": "requested",
          "approved_at": null
          """,
        ChannelRelationships
    );

    private static string PayloadFor(string type) => type switch
    {
        "webhook_issues" => IssuesPayload,
        "webhook_delivered" => DeliveredPayload,
        "webhook_undeliverable" => UndeliverablePayload,
        "webhook_channel_subscriptions" => ChannelSubscriptionPayload,
        _ => SentPayload,
    };

    private static string Sign(string payload, string signingKey = SigningKey) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), Encoding.UTF8.GetBytes(payload)));

    [Theory]
    [InlineData("webhook_issues", typeof(WebhookIssueEvent))]
    [InlineData("webhook_sent", typeof(WebhookSentEvent))]
    [InlineData("webhook_delivered", typeof(WebhookDeliveredEvent))]
    [InlineData("webhook_undeliverable", typeof(WebhookUndeliverableEvent))]
    [InlineData("webhook_channel_subscriptions", typeof(WebhookChannelSubscriptionEvent))]
    public void When_a_signed_payload_arrives_ConstructEvent_returns_the_event_its_type_names(string type, Type expected)
    {
        // Arrange
        var payload = PayloadFor(type);

        // Act
        var @event = PingenWebhook.ConstructEvent(payload, Sign(payload), SigningKey);

        // Assert
        @event.Should().BeOfType(expected);
        @event.Id.Should().Be(EventId);
        @event.Type.Should().Be(type);
        @event.Url.Should().Be("https://acme.example.com/hooks/pingen");
        @event.CreatedAt.Should().Be(CreatedAt);
        @event.Organisation!.Data!.Id.Should().Be("6c3d1f0a-1111-4000-8000-000000000001");
    }

    [Fact]
    public void When_a_delivery_runs_into_a_problem_ConstructEvent_maps_the_reason_and_the_deliverable_it_happened_on()
    {
        // Arrange
        var payload = IssuesPayload;

        // Act
        var @event = PingenWebhook.ConstructEvent(payload, Sign(payload), SigningKey);

        // Assert
        var issue = @event.Should().BeOfType<WebhookIssueEvent>().Which;
        issue.Reason.Should().Be("Content failed inspection");
        issue.Deliverable!.Data!.Id.Should().Be("2b4c6d8e-2222-4000-8000-000000000002");
        issue.Deliverable.Data.Type.Should().Be("letters");
        issue.Event!.Data!.Type.Should().Be("letters_events");
    }

    [Fact]
    public void When_a_delivery_comes_back_ConstructEvent_maps_the_reason_and_the_corrected_address()
    {
        // Arrange
        var payload = UndeliverablePayload;

        // Act
        var @event = PingenWebhook.ConstructEvent(payload, Sign(payload), SigningKey);

        // Assert
        var undeliverable = @event.Should().BeOfType<WebhookUndeliverableEvent>().Which;
        undeliverable.Reason.Should().Be("Recipient could not be determined at the specified address.");
        undeliverable.CorrectedAddress!.Name.Should().Be("Alex Meier 🇨🇭");
        undeliverable.CorrectedAddress.City.Should().Be("Zürich");
        undeliverable.CorrectedAddress.Number.Should().Be("50A");
        undeliverable.Event!.Data!.Id.Should().Be("7b1f9c22-3333-4000-8000-000000000003");
    }

    [Fact]
    public void When_a_recipient_asks_for_ebills_ConstructEvent_maps_the_subscription_and_its_channel()
    {
        // Arrange
        var payload = ChannelSubscriptionPayload;

        // Act
        var @event = PingenWebhook.ConstructEvent(payload, Sign(payload), SigningKey);

        // Assert
        var subscription = @event.Should().BeOfType<WebhookChannelSubscriptionEvent>().Which;
        subscription.Identifier.Should().Be("41020580424610132");
        subscription.Email.Should().Be("jürgen@example.com");
        subscription.Name.Should().Be("Jürgen Zürcher");
        subscription.Address.Should().Be("Hauptstrasse 24\n4001 Zürich");
        subscription.Status.Should().Be("requested");
        subscription.ApprovedAt.Should().BeNull();
        subscription.ChannelEbill!.Data!.Type.Should().Be("channel_ebills");
    }

    [Fact]
    public void Given_a_payload_carrying_unicode_When_it_is_verified_VerifySignature_accepts_the_exact_bytes_and_rejects_every_change()
    {
        // Arrange
        var signature = Sign(UndeliverablePayload);

        // Act
        var accepted = PingenWebhook.VerifySignature(UndeliverablePayload, signature, SigningKey);
        var tampered = PingenWebhook.VerifySignature(UndeliverablePayload.Replace("Zürich", "Zurich"), signature, SigningKey);
        var reformatted = PingenWebhook.VerifySignature(UndeliverablePayload.Replace("\n", ""), signature, SigningKey);
        var wrongKey = PingenWebhook.VerifySignature(UndeliverablePayload, signature, "0d1d2ae896f985c0fff1ad51d09a095a");
        var wrongCase = PingenWebhook.VerifySignature(UndeliverablePayload, signature.ToUpperInvariant(), SigningKey);

        // Assert
        accepted.Should().BeTrue();
        tampered.Should().BeFalse();
        reformatted.Should().BeFalse();
        wrongKey.Should().BeFalse();
        wrongCase.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-signature")]
    [InlineData("abc")]
    [InlineData("d09a095a0d1d2ae896f985c0fff1ad51")]
    public void When_the_signature_header_is_not_a_sha256_hex_digest_VerifySignature_rejects_it(string signatureHeader)
    {
        // Arrange
        var payload = SentPayload;

        // Act
        var accepted = PingenWebhook.VerifySignature(payload, signatureHeader, SigningKey);

        // Assert
        accepted.Should().BeFalse();
    }

    [Fact]
    public void When_the_signature_was_made_with_another_key_ConstructEvent_throws_while_ParseEvent_still_parses()
    {
        // Arrange
        var payload = SentPayload;
        var signature = Sign(payload, "0d1d2ae896f985c0fff1ad51d09a095a");

        // Act
        var act = () => PingenWebhook.ConstructEvent(payload, signature, SigningKey);

        // Assert
        act.Should().Throw<PingenException>().Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        PingenWebhook.ParseEvent(payload).Should().BeOfType<WebhookSentEvent>().Which.Deliverable!.Data!.Type.Should().Be("letters");
    }

    [Theory]
    [InlineData("""{"meta":{}}""")]
    [InlineData("""{"data":{"type":"webhook_sent","attributes":{}}}""")]
    [InlineData("""{"data":{"id":"0a1b2c3d-9999-4000-8000-000000000009","type":"webhook_sent"}}""")]
    [InlineData("""{"data":{"id":"not-a-guid","type":"webhook_sent","attributes":{}}}""")]
    [InlineData("not json at all")]
    public void When_the_payload_is_malformed_ParseEvent_throws_a_PingenException(string payload)
    {
        // Act
        var act = () => PingenWebhook.ParseEvent(payload);

        // Assert
        act.Should().Throw<PingenException>().Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void When_a_signed_payload_is_malformed_ConstructEvent_rejects_it_rather_than_letting_a_json_error_out()
    {
        // Arrange
        var payload = """{"data":{"type":"webhook_sent","attributes":{}}}""";

        // Act
        var act = () => PingenWebhook.ConstructEvent(payload, Sign(payload), SigningKey);

        // Assert
        act.Should().Throw<PingenException>().Which.Errors.Should().ContainSingle().Which.Title.Should().Be("The webhook payload was rejected");
    }

    [Fact]
    public void When_the_payload_carries_an_unknown_type_ConstructEvent_throws_naming_it()
    {
        // Arrange
        var payload = Payload("webhook_returned", BaseAttributes);

        // Act
        var act = () => PingenWebhook.ConstructEvent(payload, Sign(payload), SigningKey);

        // Assert
        act.Should().Throw<PingenException>().Which.Errors.Should().ContainSingle().Which.Detail.Should().Contain("webhook_returned");
    }
}
