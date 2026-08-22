using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>A JSON:API document carrying one resource.</summary>
public record SingleDocument<TResource>
{
    /// <summary>The resource the request addressed.</summary>
    [JsonPropertyName("data")]
    public required TResource Data { get; init; }

    /// <summary>Resources pulled in via <c>include</c>, kept raw since their shape varies per request.</summary>
    [JsonPropertyName("included")]
    public IReadOnlyList<JsonElement>? Included { get; init; }
}
