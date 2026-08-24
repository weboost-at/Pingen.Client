using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Webhooks.Payloads;

/// <summary>
/// The payload of the <c>channel_subscriptions</c> category - a recipient asked to receive ebills through the
/// organisation's channel, or stopped doing so.
/// </summary>
public record WebhookChannelSubscriptionEvent : WebhookEvent
{
    /// <summary>
    /// The e-billing identifier of the recipient.
    /// </summary>
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    /// <summary>
    /// The email address of the recipient.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// The name of the recipient.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The multiline address of the recipient.
    /// </summary>
    [JsonPropertyName("address")]
    public required string Address { get; init; }

    /// <summary>
    /// Where the subscription stands - the values are named on <see cref="ChannelSubscriptionStatus"/>.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// The instant the subscription was approved, null while it is still requested.
    /// </summary>
    [JsonPropertyName("approved_at")]
    public DateTimeOffset? ApprovedAt { get; init; }

    /// <summary>
    /// The ebill channel the recipient subscribed to.
    /// </summary>
    [JsonIgnore]
    public Relationship? ChannelEbill { get; init; }
}
