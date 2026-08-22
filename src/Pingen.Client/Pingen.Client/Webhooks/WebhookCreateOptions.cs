using System.Text.Json.Serialization;

namespace Pingen.Client.Webhooks;

/// <summary>What a webhook subscription is created from - webhooks have no update endpoint, so changing one means deleting it and creating it again.</summary>
public record WebhookCreateOptions
{
    /// <summary>The class of events the webhook subscribes to.</summary>
    [JsonPropertyName("event_category")]
    public required WebhookEventCategory EventCategory { get; init; }

    /// <summary>The URL Pingen posts the payloads to - at most 200 characters.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>The key every payload is signed with - 20 to 32 characters, and the same key <c>PingenWebhook.VerifySignature</c> is called with.</summary>
    [JsonPropertyName("signing_key")]
    public required string SigningKey { get; init; }
}
