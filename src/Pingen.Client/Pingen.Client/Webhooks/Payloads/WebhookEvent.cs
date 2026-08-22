using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Webhooks.Payloads;

/// <summary>What every payload Pingen posts to a subscribed URL carries.</summary>
public abstract record WebhookEvent
{
    // The payload records read the attributes object of the envelope - PingenWebhook fills the members the envelope carries around it.

    /// <summary>The id of the event - delivery is at-least-once, so deduplicate on it.</summary>
    [JsonIgnore]
    public Guid Id { get; init; }

    /// <summary>The JSON:API type the payload was dispatched by, for example <c>webhook_sent</c>.</summary>
    [JsonIgnore]
    public string Type { get; init; } = string.Empty;

    /// <summary>The organisation the event happened in.</summary>
    [JsonIgnore]
    public Relationship? Organisation { get; init; }

    /// <summary>The URL Pingen posted the payload to.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>The instant the event was created.</summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>What the payloads about a letter, an email or an ebill carry on top of the base shape.</summary>
public abstract record WebhookDeliverableEvent : WebhookEvent
{
    /// <summary>The letter, email or ebill the event happened on - its <c>Data.Type</c> names the channel.</summary>
    [JsonIgnore]
    public Relationship? Deliverable { get; init; }

    /// <summary>The delivery event the payload was raised for.</summary>
    [JsonIgnore]
    public Relationship? Event { get; init; }
}
