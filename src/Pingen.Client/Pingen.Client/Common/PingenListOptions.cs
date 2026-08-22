namespace Pingen.Client.Common;

/// <summary>
/// Pagination, sorting, filtering and shaping applied to a list endpoint.
/// </summary>
public record PingenListOptions
{
    /// <summary>
    /// The 1-based page to fetch - default is <c>1</c>.
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// The page size - default is <c>20</c>, maximum <c>100</c>.
    /// </summary>
    public int? PageLimit { get; init; }

    /// <summary>
    /// Comma-separated sort fields, each optionally prefixed with <c>-</c> for descending order - default is
    /// <c>created_at</c> on resource lists and <c>real_id</c> on event lists.
    /// </summary>
    public string? Sort { get; init; }

    /// <summary>
    /// The filter expression narrowing the list.
    /// </summary>
    public PingenFilter? Filter { get; init; }

    /// <summary>
    /// The full-text search term.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Comma-separated to-one relationships to embed in the response.
    /// </summary>
    public string? Include { get; init; }

    /// <summary>
    /// The language event names are localized into - default is <c>en-GB</c>.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Sparse fieldsets keyed by JSON:API type, each a comma-separated attribute list.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Fields { get; init; }
}
