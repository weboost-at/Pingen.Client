using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Webhooks;

/// <summary>
/// A subscription Pingen posts event payloads to.
/// </summary>
public record Webhook
{
    /// <summary>
    /// The id of the webhook.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <see cref="PingenType.Webhooks"/>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// What the webhook subscribes to, where it posts and how it signs.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required WebhookAttributes Attributes { get; init; }

    /// <summary>
    /// The organisation the webhook belongs to.
    /// </summary>
    [JsonPropertyName("relationships")]
    public WebhookRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the webhook - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of a webhook.
/// </summary>
public record WebhookAttributes
{
    /// <summary>
    /// The class of events the webhook subscribes to.
    /// </summary>
    [JsonPropertyName("event_category")]
    public required WebhookEventCategory EventCategory { get; init; }

    /// <summary>
    /// The URL Pingen posts the payloads to.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// The key every payload is signed with, echoed back in cleartext.
    /// </summary>
    [JsonPropertyName("signing_key")]
    public required string SigningKey { get; init; }
}

/// <summary>
/// The resources a webhook is related to.
/// </summary>
public record WebhookRelationships
{
    /// <summary>
    /// The organisation whose events the webhook receives.
    /// </summary>
    [JsonPropertyName("organisation")]
    public Relationship? Organisation { get; init; }
}
