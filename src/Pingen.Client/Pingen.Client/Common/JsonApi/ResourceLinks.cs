using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>The links a single resource carries.</summary>
public record ResourceLinks
{
    /// <summary>The canonical URL of the resource.</summary>
    [JsonPropertyName("self")]
    public string? Self { get; init; }
}
