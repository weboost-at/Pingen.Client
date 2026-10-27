using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pingen.Client.Webhooks;

namespace Pingen.Client.Tests.Webhooks.Support;

public abstract class WebhookTestBase : TestBase
{
    protected WebhookTestData Webhook => Data.Webhook;

    protected WebhookService Webhooks => Resolve<WebhookService>();

    protected string WebhooksPath => $"/organisations/{Organisation.Id}/webhooks";

    protected string WebhookPath => $"{WebhooksPath}/{Webhook.Id}";

    protected string WebhookDocument(string eventCategory) =>
        $$"""{ "data": {{WebhookResource(eventCategory, abilities: true)}} }""";

    protected string WebhookPage() =>
        $$"""
          {
            "data": [ {{WebhookResource("channel_subscriptions", abilities: false)}} ],
            "links": { "self": "https://api.pingen.com{{WebhooksPath}}" },
            "meta": { "current_page": 1, "last_page": 1, "per_page": 20, "from": 1, "to": 1, "total": 1 }
          }
          """;

    protected string Payload(string? type) =>
        type switch
        {
            "webhook_issues" => IssuesPayload(),
            "webhook_undeliverable" => UndeliverablePayload(),
            "webhook_channel_subscriptions" => ChannelSubscriptionPayload(),
            _ => EventDocument(type, EventAttributes, DeliverableRelationships)
        };

    protected string SentPayload() => Payload("webhook_sent");

    protected string IssuesPayload() =>
        EventDocument(
            "webhook_issues",
            $$"""
              {{EventAttributes}},
              "reason": "{{Webhook.IssueReason}}"
              """,
            DeliverableRelationships
        );

    protected string UndeliverablePayload() =>
        EventDocument(
            "webhook_undeliverable",
            $$"""
              {{EventAttributes}},
              "reason": "{{Webhook.UndeliverableReason}}",
              "corrected_address": {
                "name": "{{Webhook.CorrectedAddress.Name}}",
                "street": "{{Webhook.CorrectedAddress.Street}}",
                "number": "{{Webhook.CorrectedAddress.Number}}",
                "zip": "{{Webhook.CorrectedAddress.Zip}}",
                "city": "{{Webhook.CorrectedAddress.City}}"
              }
              """,
            DeliverableRelationships
        );

    protected string ChannelSubscriptionPayload() =>
        EventDocument(
            "webhook_channel_subscriptions",
            $$"""
              {{EventAttributes}},
              "identifier": "{{Webhook.Identifier}}",
              "email": "{{Webhook.Email}}",
              "name": "{{Webhook.Name}}",
              "address": "Hauptstrasse 24\n4001 Zürich",
              "status": "requested",
              "approved_at": null
              """,
            ChannelRelationships
        );

    protected string Sign(string payload) => Sign(payload, Webhook.SigningKey);

    protected static string Sign(string payload, string signingKey) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), Encoding.UTF8.GetBytes(payload)));

    private string WebhookResource(string eventCategory, bool abilities) =>
        $$"""
          {
            "id": "{{Webhook.Id}}",
            "type": "webhooks",
            "attributes": {
              "event_category": "{{eventCategory}}",
              "url": "{{Webhook.Url}}",
              "signing_key": "{{Webhook.SigningKey}}"
            },
            "relationships": {
              "organisation": { "data": { "id": "{{Organisation.Id}}", "type": "organisations" } }
            },
            "links": { "self": "https://api.pingen.com{{WebhookPath}}" }{{(abilities ? AbilitiesMeta : "")}}
          }
          """;

    private string EventAttributes =>
        $$"""
          "url": "{{Webhook.Url}}",
          "created_at": "2020-11-19T09:42:48+0100"
          """;

    private string DeliverableRelationships =>
        $$"""
          "organisation": { "data": { "id": "{{Organisation.Id}}", "type": "organisations" } },
          "deliverable": { "data": { "id": "{{Webhook.DeliverableId}}", "type": "letters" } },
          "event": { "data": { "id": "{{Webhook.DeliveryEventId}}", "type": "letters_events" } }
          """;

    private string ChannelRelationships =>
        $$"""
          "organisation": { "data": { "id": "{{Organisation.Id}}", "type": "organisations" } },
          "channel_ebill": { "data": { "id": "{{Webhook.ChannelEbillId}}", "type": "channel_ebills" } }
          """;

    // Serialized, so a null type reaches the wire as a JSON null rather than a string
    private string EventDocument(string? type, string attributes, string relationships) =>
        $$"""
          {
            "data": {
              "id": "{{Webhook.EventId}}",
              "type": {{JsonSerializer.Serialize(type)}},
              "attributes": { {{attributes}} },
              "relationships": { {{relationships}} },
              "links": { "self": "https://api.pingen.com/webhooks/{{Webhook.EventId}}" }
            }
          }
          """;

    private const string AbilitiesMeta =
        """
        , "meta": { "abilities": { "self": { "delete": "ok" } } }
        """;
}
