using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.Ebills;

/// <summary>
/// The invoice details of an ebill delivery - the API requires all four members whenever meta data is sent at all.
/// </summary>
public record EbillMetaData
{
    /// <summary>
    /// The invoice number the recipient reconciles the ebill by - at most 100 characters.
    /// </summary>
    [JsonPropertyName("invoice_number")]
    public required string InvoiceNumber { get; init; }

    /// <summary>
    /// The day the invoice was issued.
    /// </summary>
    [JsonPropertyName("invoice_date")]
    public required DateOnly InvoiceDate { get; init; }

    /// <summary>
    /// The day the invoice falls due.
    /// </summary>
    [JsonPropertyName("invoice_due_date")]
    public required DateOnly InvoiceDueDate { get; init; }

    /// <summary>
    /// The e-billing identifier of the recipient.
    /// </summary>
    [JsonPropertyName("recipient_identifier")]
    public required string RecipientIdentifier { get; init; }
}
