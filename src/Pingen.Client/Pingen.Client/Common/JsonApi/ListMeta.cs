using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>
/// The pagination counters a list response carries.
/// </summary>
public record ListMeta
{
    /// <summary>
    /// The 1-based number of this page.
    /// </summary>
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; init; }

    /// <summary>
    /// The number of the last page of the collection.
    /// </summary>
    [JsonPropertyName("last_page")]
    public int LastPage { get; init; }

    /// <summary>
    /// The page size the API applied.
    /// </summary>
    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    /// <summary>
    /// The 1-based index of the first item on this page, null when the page is empty.
    /// </summary>
    [JsonPropertyName("from")]
    public int? From { get; init; }

    /// <summary>
    /// The 1-based index of the last item on this page, null when the page is empty.
    /// </summary>
    [JsonPropertyName("to")]
    public int? To { get; init; }

    /// <summary>
    /// The number of items in the whole collection.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; init; }
}
