using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Batches;

/// <summary>
/// A batch Pingen splits into deliveries and dispatches through one channel.
/// </summary>
public record Batch
{
    /// <summary>
    /// The id of the batch.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <see cref="PingenType.Batches"/>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// What the batch contains, what it costs and how far it got.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required BatchAttributes Attributes { get; init; }

    /// <summary>
    /// The organisation and the events the batch belongs to.
    /// </summary>
    [JsonPropertyName("relationships")]
    public BatchRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the batch - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of a batch.
/// </summary>
public record BatchAttributes
{
    /// <summary>
    /// The name the batch was filed under.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The channel the batch is dispatched through - <c>post</c>, <c>ebill</c> or <c>email</c>.
    /// </summary>
    [JsonPropertyName("channel_type")]
    public required string ChannelType { get; init; }

    /// <summary>
    /// The icon the batch is shown with, for example <c>wave-hand</c>.
    /// </summary>
    [JsonPropertyName("icon")]
    public required string Icon { get; init; }

    /// <summary>
    /// How far the batch got - Pingen deliberately publishes no complete list of statuses.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// The file name the batch was uploaded under.
    /// </summary>
    [JsonPropertyName("file_original_name")]
    public required string FileOriginalName { get; init; }

    /// <summary>
    /// The number of letters the batch was split into.
    /// </summary>
    [JsonPropertyName("letter_count")]
    public required int LetterCount { get; init; }

    /// <summary>
    /// The number of deliveries the batch was split into.
    /// </summary>
    [JsonPropertyName("deliverable_count")]
    public required int DeliverableCount { get; init; }

    /// <summary>
    /// Which window the recipient addresses show through - <c>left</c> or <c>right</c>.
    /// </summary>
    [JsonPropertyName("address_position")]
    public required string AddressPosition { get; init; }

    /// <summary>
    /// Which sides of the paper are printed - <c>simplex</c> or <c>duplex</c>.
    /// </summary>
    [JsonPropertyName("print_mode")]
    public required string PrintMode { get; init; }

    /// <summary>
    /// Which colors are printed - <c>color</c> or <c>grayscale</c>.
    /// </summary>
    [JsonPropertyName("print_spectrum")]
    public required string PrintSpectrum { get; init; }

    /// <summary>
    /// The currency the price is quoted in.
    /// </summary>
    [JsonPropertyName("price_currency")]
    public required string PriceCurrency { get; init; }

    /// <summary>
    /// What the batch costs.
    /// </summary>
    [JsonPropertyName("price_value")]
    public required decimal PriceValue { get; init; }

    /// <summary>
    /// Where the batch came from - an open set including <c>app</c> and <c>api</c>.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// The instant the batch was handed to production, null while it is still waiting to be sent.
    /// </summary>
    [JsonPropertyName("submitted_at")]
    public DateTimeOffset? SubmittedAt { get; init; }

    /// <summary>
    /// The instant the batch was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The instant the batch was last changed.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The resources a batch is related to.
/// </summary>
public record BatchRelationships
{
    /// <summary>
    /// The organisation the batch belongs to.
    /// </summary>
    [JsonPropertyName("organisation")]
    public Relationship? Organisation { get; init; }

    /// <summary>
    /// The events recorded on the batch, exposed as a link and a count instead of embedded identities.
    /// </summary>
    [JsonPropertyName("events")]
    public RelatedCollection? Events { get; init; }
}
