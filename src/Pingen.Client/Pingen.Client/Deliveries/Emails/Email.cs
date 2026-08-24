using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Deliveries.Emails;

/// <summary>
/// An email delivery - a PDF Pingen mails to a recipient address instead of printing it.
/// </summary>
public record Email
{
    /// <summary>
    /// The id of the email.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <see cref="PingenType.Emails"/>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The state, file, price and timestamps of the email.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required EmailAttributes Attributes { get; init; }

    /// <summary>
    /// The organisation, batch and events the email belongs to.
    /// </summary>
    [JsonPropertyName("relationships")]
    public EmailRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the email - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of an email delivery.
/// </summary>
public record EmailAttributes
{
    /// <summary>
    /// Where the email stands - Pingen deliberately publishes no complete list; observed values are validating, valid,
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
    /// The email address the delivery is addressed to.
    /// </summary>
    [JsonPropertyName("recipient_identifier")]
    public required string RecipientIdentifier { get; init; }

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
    /// Where the email entered Pingen - an open set including app, api, batch and the integration_* sources.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// The instant the email was submitted for delivery, null while it is still being prepared.
    /// </summary>
    [JsonPropertyName("submitted_at")]
    public DateTimeOffset? SubmittedAt { get; init; }

    /// <summary>
    /// The instant the email was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The instant the email was last changed.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The relationships of an email delivery.
/// </summary>
public record EmailRelationships
{
    /// <summary>
    /// The organisation the email was sent from.
    /// </summary>
    [JsonPropertyName("organisation")]
    public Relationship? Organisation { get; init; }

    /// <summary>
    /// The batch the email was created by, absent on emails created one by one.
    /// </summary>
    [JsonPropertyName("batch")]
    public Relationship? Batch { get; init; }

    /// <summary>
    /// The events of the email, which Pingen exposes as a link and a count instead of embedded identities.
    /// </summary>
    [JsonPropertyName("events")]
    public RelatedCollection? Events { get; init; }
}
