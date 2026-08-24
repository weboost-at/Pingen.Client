using System.Text.Json.Serialization;
using Pingen.Client.Common.Json;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Deliveries.Letters;

/// <summary>
/// A letter Pingen prints and mails.
/// </summary>
public record Letter
{
    /// <summary>
    /// The id of the letter.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <see cref="PingenType.Letters"/>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// What the letter contains, what it costs and how far it got.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required LetterAttributes Attributes { get; init; }

    /// <summary>
    /// The organisation, the batch and the events the letter belongs to.
    /// </summary>
    [JsonPropertyName("relationships")]
    public LetterRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the letter - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of a letter.
/// </summary>
public record LetterAttributes
{
    /// <summary>
    /// How far the letter got - the observed values are named on <see cref="LetterStatus"/>, and Pingen deliberately
    /// publishes no complete list.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// The file name the letter was filed under.
    /// </summary>
    [JsonPropertyName("file_original_name")]
    public required string FileOriginalName { get; init; }

    /// <summary>
    /// The number of pages of the PDF.
    /// </summary>
    [JsonPropertyName("file_pages")]
    public required int FilePages { get; init; }

    /// <summary>
    /// The recipient address read from the address window, one line per newline.
    /// </summary>
    [JsonPropertyName("address")]
    public required string Address { get; init; }

    /// <summary>
    /// Which window the recipient address shows through - <c>left</c> or <c>right</c>.
    /// </summary>
    [JsonPropertyName("address_position")]
    public required string AddressPosition { get; init; }

    /// <summary>
    /// The ISO 3166-1 alpha-2 country the letter is delivered to.
    /// </summary>
    [JsonPropertyName("country")]
    public required string Country { get; init; }

    /// <summary>
    /// The product the letter is dispatched with - <c>fast</c>, <c>cheap</c>, <c>bulk</c>, <c>premium</c> or
    /// <c>registered</c>, plus the electronic products a batch channel assigns.
    /// </summary>
    [JsonPropertyName("delivery_product")]
    public required string DeliveryProduct { get; init; }

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
    /// What the letter costs.
    /// </summary>
    [JsonPropertyName("price_value")]
    public required decimal PriceValue { get; init; }

    /// <summary>
    /// The kind of paper each page is printed on, one entry per page.
    /// </summary>
    [JsonPropertyName("paper_types")]
    public required IReadOnlyList<string> PaperTypes { get; init; }

    /// <summary>
    /// The fonts found in the PDF.
    /// </summary>
    [JsonPropertyName("fonts")]
    public required IReadOnlyList<LetterFont> Fonts { get; init; }

    /// <summary>
    /// Where the letter came from - the values the spec declares are named on <see cref="DeliverySource"/>.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// The tracking number of the shipment, set once a tracked product left the print centre.
    /// </summary>
    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; init; }

    /// <summary>
    /// The instant the letter was handed to production, null while it is still waiting to be sent.
    /// </summary>
    [JsonPropertyName("submitted_at")]
    public DateTimeOffset? SubmittedAt { get; init; }

    /// <summary>
    /// The instant the letter was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The instant the letter was last changed.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The resources a letter is related to.
/// </summary>
public record LetterRelationships
{
    /// <summary>
    /// The organisation the letter belongs to.
    /// </summary>
    [JsonPropertyName("organisation")]
    public Relationship? Organisation { get; init; }

    /// <summary>
    /// The batch the letter was created by, absent on letters created on their own.
    /// </summary>
    [JsonPropertyName("batch")]
    public Relationship? Batch { get; init; }

    /// <summary>
    /// The events recorded on the letter, exposed as a link and a count instead of embedded identities.
    /// </summary>
    [JsonPropertyName("events")]
    public RelatedCollection? Events { get; init; }
}

/// <summary>
/// A font the PDF of a letter uses.
/// </summary>
public record LetterFont
{
    /// <summary>
    /// The name of the font, for example <c>Helvetica</c>.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Whether the font travels inside the PDF - a font that does not is substituted at print time.
    /// </summary>
    [JsonPropertyName("is_embedded")]
    [JsonConverter(typeof(PingenBooleanConverter))]
    public required bool IsEmbedded { get; init; }
}
