using System.Text.Json.Serialization;

namespace Pingen.Client.Webhooks.Payloads;

/// <summary>The payload of the <c>undeliverable</c> category - a delivery came back instead of reaching its recipient.</summary>
public record WebhookUndeliverableEvent : WebhookDeliverableEvent
{
    /// <summary>Why the delivery failed, for example <c>Recipient could not be determined at the specified address.</c>.</summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>The address the postal service corrected the recipient to, null when it reported none.</summary>
    [JsonPropertyName("corrected_address")]
    public WebhookCorrectedAddress? CorrectedAddress { get; init; }
}

/// <summary>The address a postal service returned along with an undeliverable delivery.</summary>
public record WebhookCorrectedAddress
{
    /// <summary>The name of the recipient.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The street of the corrected address.</summary>
    [JsonPropertyName("street")]
    public string? Street { get; init; }

    /// <summary>The house number of the corrected address.</summary>
    [JsonPropertyName("number")]
    public string? Number { get; init; }

    /// <summary>The postal code of the corrected address.</summary>
    [JsonPropertyName("zip")]
    public string? Zip { get; init; }

    /// <summary>The city of the corrected address.</summary>
    [JsonPropertyName("city")]
    public string? City { get; init; }
}
