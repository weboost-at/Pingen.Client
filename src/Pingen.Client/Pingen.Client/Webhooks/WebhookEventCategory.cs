using System.Text.Json.Serialization;

namespace Pingen.Client.Webhooks;

/// <summary>The class of events a webhook subscribes to - one webhook carries exactly one category.</summary>
public enum WebhookEventCategory
{
    /// <summary>Problems Pingen ran into while processing a delivery.</summary>
    [JsonStringEnumMemberName("issues")]
    Issues,

    /// <summary>Deliveries that left Pingen.</summary>
    [JsonStringEnumMemberName("sent")]
    Sent,

    /// <summary>Deliveries that could not be handed to the recipient.</summary>
    [JsonStringEnumMemberName("undeliverable")]
    Undeliverable,

    /// <summary>Deliveries that reached the recipient.</summary>
    [JsonStringEnumMemberName("delivered")]
    Delivered,

    /// <summary>Recipients subscribing to or unsubscribing from an ebill channel.</summary>
    [JsonStringEnumMemberName("channel_subscriptions")]
    ChannelSubscriptions,
}
