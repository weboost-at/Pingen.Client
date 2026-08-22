using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Batches;

/// <summary>
/// How the letters of a batch are distributed across validation groups, countries and regions.
/// </summary>
public record BatchStatistics
{
    /// <summary>
    /// The id of the statistics resource, which repeats the id of the batch.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <c>batch_details_statistics</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The counted distributions.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required BatchStatisticsAttributes Attributes { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the statistics.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of the statistics of a batch.
/// </summary>
public record BatchStatisticsAttributes
{
    /// <summary>
    /// How many letters of the batch are still being validated.
    /// </summary>
    [JsonPropertyName("letter_validating")]
    public required int LetterValidating { get; init; }

    /// <summary>
    /// How many letters fall into each validation group, for example <c>valid</c> or <c>not_available</c>.
    /// </summary>
    [JsonPropertyName("letter_groups")]
    public required IReadOnlyList<BatchLetterGroup> LetterGroups { get; init; }

    /// <summary>
    /// How many letters go to each country.
    /// </summary>
    [JsonPropertyName("letter_countries")]
    public required IReadOnlyList<BatchLetterCountry> LetterCountries { get; init; }

    /// <summary>
    /// How many letters go to each region, counted by the country the region belongs to.
    /// </summary>
    [JsonPropertyName("letter_regions")]
    public required IReadOnlyList<BatchLetterCountry> LetterRegions { get; init; }
}

/// <summary>
/// How many letters of a batch share one validation group.
/// </summary>
public record BatchLetterGroup
{
    /// <summary>
    /// The name of the group.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// How many letters are in the group.
    /// </summary>
    [JsonPropertyName("count")]
    public required int Count { get; init; }
}

/// <summary>
/// How many letters of a batch go to one country or region.
/// </summary>
public record BatchLetterCountry
{
    /// <summary>
    /// The ISO 3166-1 alpha-2 country.
    /// </summary>
    [JsonPropertyName("country")]
    public required string Country { get; init; }

    /// <summary>
    /// How many letters go there.
    /// </summary>
    [JsonPropertyName("count")]
    public required int Count { get; init; }
}
