using System.Text.Json.Serialization;

namespace Pingen.Client.Common;

/// <summary>
/// One error entry of a failed Pingen response.
/// </summary>
public record PingenError
{
    /// <summary>
    /// The opaque error code Pingen assigned - the set is undocumented.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>
    /// The short summary of what went wrong.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// The detailed description of what went wrong.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>
    /// The part of the request that caused the error.
    /// </summary>
    [JsonPropertyName("source")]
    public PingenErrorSource? Source { get; init; }
}

/// <summary>
/// Points at the request member an error refers to.
/// </summary>
public record PingenErrorSource
{
    /// <summary>
    /// The JSON pointer into the request body, for example <c>/data/attributes/file_url</c>.
    /// </summary>
    [JsonPropertyName("pointer")]
    public string? Pointer { get; init; }

    /// <summary>
    /// The query parameter the error refers to.
    /// </summary>
    [JsonPropertyName("parameter")]
    public string? Parameter { get; init; }
}

/// <summary>
/// The body every 4XX and 5XX response carries.
/// </summary>
public record PingenErrorDocument
{
    /// <summary>
    /// The errors the API reported.
    /// </summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<PingenError> Errors { get; init; } = [];
}
