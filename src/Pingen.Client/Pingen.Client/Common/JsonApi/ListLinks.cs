using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>The pagination links a list response carries.</summary>
public record ListLinks
{
    /// <summary>The first page of the collection.</summary>
    [JsonPropertyName("first")]
    public string? First { get; init; }

    /// <summary>The last page of the collection.</summary>
    [JsonPropertyName("last")]
    public string? Last { get; init; }

    /// <summary>The page before this one, absent on the first page.</summary>
    [JsonPropertyName("prev")]
    public string? Prev { get; init; }

    /// <summary>The page after this one, absent on the last page.</summary>
    [JsonPropertyName("next")]
    public string? Next { get; init; }

    /// <summary>This page.</summary>
    [JsonPropertyName("self")]
    public string? Self { get; init; }
}
