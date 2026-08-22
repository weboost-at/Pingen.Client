using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.Letters;

/// <summary>
/// The address blocks Pingen prints onto a letter instead of reading them from the PDF.
/// </summary>
public record LetterMetaData
{
    /// <summary>
    /// Who receives the letter.
    /// </summary>
    [JsonPropertyName("recipient")]
    public required LetterAddress Recipient { get; init; }

    /// <summary>
    /// Who sends the letter.
    /// </summary>
    [JsonPropertyName("sender")]
    public required LetterAddress Sender { get; init; }
}

/// <summary>
/// One address block of a letter - every member is optional so both the street and the PO box shape are expressible,
/// which is what the field descriptions ask for even though the schema marks all of them required.
/// </summary>
public record LetterAddress
{
    /// <summary>
    /// The name of the person or company - at most 45 characters.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The street, given instead of <see cref="PoBox"/> - at most 40 characters.
    /// </summary>
    [JsonPropertyName("street")]
    public string? Street { get; init; }

    /// <summary>
    /// The PO box, given instead of <see cref="Street"/> - at most 45 characters.
    /// </summary>
    [JsonPropertyName("pobox")]
    public string? PoBox { get; init; }

    /// <summary>
    /// The house number - at most 10 characters.
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; init; }

    /// <summary>
    /// The postal code - at most 8 characters.
    /// </summary>
    [JsonPropertyName("zip")]
    public string? Zip { get; init; }

    /// <summary>
    /// The city - at most 25 characters.
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; init; }

    /// <summary>
    /// The ISO 3166-1 alpha-2 country code - exactly 2 characters.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }
}
