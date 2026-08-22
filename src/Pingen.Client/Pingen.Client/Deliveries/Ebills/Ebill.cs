using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Deliveries.Ebills;

/// <summary>
/// An ebill delivery - an invoice PDF Pingen hands to the recipient's e-billing provider.
/// </summary>
public record Ebill
{
    /// <summary>
    /// The id of the ebill.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <c>ebills</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The state, file, invoice, price and timestamps of the ebill.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required EbillAttributes Attributes { get; init; }

    /// <summary>
    /// The organisation, batch and events the ebill belongs to.
    /// </summary>
    [JsonPropertyName("relationships")]
    public EbillRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the ebill - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of an ebill delivery.
/// </summary>
public record EbillAttributes
{
    /// <summary>
    /// Where the ebill stands - Pingen deliberately publishes no complete list; observed values are validating, valid,
    /// invalid, action_required, fixing, submitted, awaiting_credits, accepted, inspection, processing, sent,
    /// delivered, undeliverable, rejected, expired, cancelling, cancelled.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// The name the uploaded file was created under.
    /// </summary>
    [JsonPropertyName("file_original_name")]
    public required string FileOriginalName { get; init; }

    /// <summary>
    /// The number of pages of the uploaded file.
    /// </summary>
    [JsonPropertyName("file_pages")]
    public required int FilePages { get; init; }

    /// <summary>
    /// The e-billing identifier of the recipient.
    /// </summary>
    [JsonPropertyName("recipient_identifier")]
    public required string RecipientIdentifier { get; init; }

    /// <summary>
    /// The multiline address of the recipient.
    /// </summary>
    [JsonPropertyName("recipient_address")]
    public required string RecipientAddress { get; init; }

    /// <summary>
    /// The invoice number the recipient reconciles the ebill by.
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
    /// The amount the invoice asks for.
    /// </summary>
    [JsonPropertyName("invoice_value")]
    public required decimal InvoiceValue { get; init; }

    /// <summary>
    /// The ISO currency the invoice is denominated in.
    /// </summary>
    [JsonPropertyName("invoice_currency")]
    public required string InvoiceCurrency { get; init; }

    /// <summary>
    /// The IBAN the invoice is payable to.
    /// </summary>
    [JsonPropertyName("invoice_iban")]
    public required string InvoiceIban { get; init; }

    /// <summary>
    /// The multiline address of the invoice issuer.
    /// </summary>
    [JsonPropertyName("invoice_address")]
    public required string InvoiceAddress { get; init; }

    /// <summary>
    /// The payment reference the invoice is paid under.
    /// </summary>
    [JsonPropertyName("invoice_reference")]
    public required string InvoiceReference { get; init; }

    /// <summary>
    /// The ISO currency the price is denominated in.
    /// </summary>
    [JsonPropertyName("price_currency")]
    public required string PriceCurrency { get; init; }

    /// <summary>
    /// What the delivery costs.
    /// </summary>
    [JsonPropertyName("price_value")]
    public required decimal PriceValue { get; init; }

    /// <summary>
    /// Where the ebill entered Pingen - an open set including app, api, batch and the integration_* sources.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// The instant the ebill was submitted for delivery, null while it is still being prepared.
    /// </summary>
    [JsonPropertyName("submitted_at")]
    public DateTimeOffset? SubmittedAt { get; init; }

    /// <summary>
    /// The instant the ebill was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The instant the ebill was last changed.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The relationships of an ebill delivery.
/// </summary>
public record EbillRelationships
{
    /// <summary>
    /// The organisation the ebill was sent from.
    /// </summary>
    [JsonPropertyName("organisation")]
    public Relationship? Organisation { get; init; }

    /// <summary>
    /// The batch the ebill was created by, absent on ebills created one by one.
    /// </summary>
    [JsonPropertyName("batch")]
    public Relationship? Batch { get; init; }

    /// <summary>
    /// The events of the ebill, which Pingen exposes as a link and a count instead of embedded identities.
    /// </summary>
    [JsonPropertyName("events")]
    public RelatedCollection? Events { get; init; }
}
