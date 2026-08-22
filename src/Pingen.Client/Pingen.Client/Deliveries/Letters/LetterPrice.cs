using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Deliveries.Letters;

/// <summary>What a letter of a given shape would cost.</summary>
public record LetterPrice
{
    /// <summary>The id of the calculation.</summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>The JSON:API type of the resource, always <c>letter_price_calculator</c>.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>The calculated price.</summary>
    [JsonPropertyName("attributes")]
    public required LetterPriceAttributes Attributes { get; init; }

    /// <summary>The canonical URL of the resource.</summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }
}

/// <summary>The attributes of a calculated letter price.</summary>
public record LetterPriceAttributes
{
    /// <summary>The currency the price is quoted in.</summary>
    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    /// <summary>What the letter would cost.</summary>
    [JsonPropertyName("price")]
    public required decimal Price { get; init; }
}
