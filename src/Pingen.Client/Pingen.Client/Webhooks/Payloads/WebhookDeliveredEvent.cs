namespace Pingen.Client.Webhooks.Payloads;

/// <summary>
/// The payload of the <c>delivered</c> category - a delivery reached its recipient.
/// </summary>
public record WebhookDeliveredEvent : WebhookDeliverableEvent;
