using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>
/// A JSON:API document carrying one page of resources.
/// </summary>
public record ListDocument<TResource>
{
    /// <summary>
    /// The resources on this page.
    /// </summary>
    [JsonPropertyName("data")]
    public required IReadOnlyList<TResource> Data { get; init; }

    /// <summary>
    /// Resources pulled in via <c>include</c>, kept raw since their shape varies per request.
    /// </summary>
    [JsonPropertyName("included")]
    public IReadOnlyList<JsonElement>? Included { get; init; }

    /// <summary>
    /// The pagination links of this page.
    /// </summary>
    [JsonPropertyName("links")]
    public ListLinks? Links { get; init; }

    /// <summary>
    /// The pagination counters of this page.
    /// </summary>
    [JsonPropertyName("meta")]
    public ListMeta? Meta { get; init; }
}
