using System.Globalization;

namespace Pingen.Client.Common;

/// <summary>Renders <see cref="PingenListOptions"/> as the query string a Pingen list endpoint reads.</summary>
public static class PingenQuery
{
    /// <summary>The query string for <paramref name="options"/> including the leading <c>?</c>, empty when nothing is set.</summary>
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
