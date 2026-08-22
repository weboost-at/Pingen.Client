using System.Text.Json.Serialization;

namespace Pingen.Client.Webhooks.Payloads;

/// <summary>The payload of the <c>issues</c> category - a delivery ran into a problem that needs attention.</summary>
public record WebhookIssueEvent : WebhookDeliverableEvent
{
    /// <summary>What went wrong, for example <c>Content failed inspection</c>.</summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
}
