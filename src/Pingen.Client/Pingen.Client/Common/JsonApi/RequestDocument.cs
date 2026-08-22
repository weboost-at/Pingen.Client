using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>
/// The JSON:API document wrapping the attributes of a write request.
/// </summary>
public record RequestDocument<TAttributes>
{
    /// <summary>
    /// The resource object being written.
    /// </summary>
    [JsonPropertyName("data")]
    public required RequestData<TAttributes> Data { get; init; }
}

/// <summary>
/// The resource object of a write request.
/// </summary>
public record RequestData<TAttributes>
{
    /// <summary>
    /// The JSON:API type being written, for example <c>letters</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The id of the resource - creates omit it, send and edit calls repeat the path id here.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>
    /// The attributes being written.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required TAttributes Attributes { get; init; }

    /// <summary>
    /// The relationships being written.
    /// </summary>
    [JsonPropertyName("relationships")]
    public RequestRelationships? Relationships { get; init; }
}

/// <summary>
/// The relationships a write request may set.
/// </summary>
public record RequestRelationships
{
    /// <summary>
    /// The preset the created resource inherits its defaults from.
    /// </summary>
    [JsonPropertyName("preset")]
    public PresetRelationship? Preset { get; init; }
}

/// <summary>
/// References the preset a create request applies.
/// </summary>
public record PresetRelationship
{
    /// <summary>
    /// Identity of the preset.
    /// </summary>
    [JsonPropertyName("data")]
    public required ResourceIdentifier Data { get; init; }
}

/// <summary>
/// Builds the write-side documents the API expects.
/// </summary>
public static class RequestDocument
{
    /// <summary>
    /// Wraps <paramref name="attributes"/> in a JSON:API document of the given type, adding the id and preset
    /// relationship when supplied.
    /// </summary>
    public static RequestDocument<TAttributes> For<TAttributes>(string type, TAttributes attributes, string? id = null, Guid? presetId = null) =>
        new()
        {
            Data = new()
            {
                Type = type,
                Id = id,
                Attributes = attributes,
                Relationships = presetId is { } preset
                    ? new() { Preset = new() { Data = new() { Id = preset.ToString(), Type = "presets" } } }
                    : null,
            },
        };
}
