using System.Globalization;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Pingen.Client.Tests")]

namespace Pingen.Client.Common;

/// <summary>Pagination, sorting, filtering and shaping applied to a list endpoint.</summary>
public record PingenListOptions
{
    /// <summary>The 1-based page to fetch - default is <c>1</c>.</summary>
    public int? PageNumber { get; init; }

    /// <summary>The page size - default is <c>20</c>, maximum <c>100</c>.</summary>
    public int? PageLimit { get; init; }

    /// <summary>Comma-separated sort fields, each optionally prefixed with <c>-</c> for descending order - default is <c>created_at</c> on resource lists and <c>real_id</c> on event lists.</summary>
    public string? Sort { get; init; }

    /// <summary>The filter expression narrowing the list.</summary>
    public PingenFilter? Filter { get; init; }

    /// <summary>The full-text search term.</summary>
    public string? Search { get; init; }

    /// <summary>Comma-separated to-one relationships to embed in the response.</summary>
    public string? Include { get; init; }

    /// <summary>The language event names are localized into - default is <c>en-GB</c>.</summary>
    public string? Language { get; init; }

    /// <summary>Sparse fieldsets keyed by JSON:API type, each a comma-separated attribute list.</summary>
    public IReadOnlyDictionary<string, string>? Fields { get; init; }
}

internal static class PingenQuery
{
    public static string Build(PingenListOptions? options)
    {
        if (options is null) return string.Empty;

        var parameters = new List<string>();
        if (options.PageNumber is { } pageNumber) parameters.Add($"page[number]={pageNumber.ToString(CultureInfo.InvariantCulture)}");
        if (options.PageLimit is { } pageLimit) parameters.Add($"page[limit]={pageLimit.ToString(CultureInfo.InvariantCulture)}");
        if (options.Sort is { Length: > 0 } sort) parameters.Add($"sort={EscapeList(sort)}");
        if (options.Filter is { } filter) parameters.Add($"filter={Uri.EscapeDataString(filter.ToJson())}");
        if (options.Search is { Length: > 0 } search) parameters.Add($"q={Uri.EscapeDataString(search)}");
        if (options.Include is { Length: > 0 } include) parameters.Add($"include={EscapeList(include)}");
        if (options.Language is { Length: > 0 } language) parameters.Add($"language={Uri.EscapeDataString(language)}");

        // Ordered by type so the same options always produce the same URL.
        if (options.Fields is { } fields)
            foreach (var (type, attributes) in fields.OrderBy(field => field.Key, StringComparer.Ordinal))
                parameters.Add($"fields[{type}]={EscapeList(attributes)}");

        return parameters.Count is 0 ? string.Empty : $"?{string.Join('&', parameters)}";
    }

    // The commas separating list values are grammar, not data - only the segments between them are escaped.
    private static string EscapeList(string value) => string.Join(',', value.Split(',').Select(Uri.EscapeDataString));
}
