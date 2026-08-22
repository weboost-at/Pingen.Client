using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.Ebills;

/// <summary>What an ebill delivery is created from.</summary>
public record EbillCreateOptions
{
    /// <summary>The name the uploaded file is recorded under - at most 255 characters.</summary>
    [JsonPropertyName("file_original_name")]
    public required string FileOriginalName { get; init; }

    /// <summary>The presigned URL the PDF was uploaded to - at most 1000 characters; the <c>Stream</c> overload of <c>CreateAsync</c> fills it from the upload it performs.</summary>
    [JsonPropertyName("file_url")]
    public string? FileUrl { get; init; }

    /// <summary>The signature of the presigned URL, copied verbatim - at most 60 characters; the <c>Stream</c> overload of <c>CreateAsync</c> fills it from the upload it performs.</summary>
    [JsonPropertyName("file_url_signature")]
    public string? FileUrlSignature { get; init; }

    /// <summary>Whether the ebill is dispatched as soon as it validates instead of waiting for <c>SendAsync</c>.</summary>
    [JsonPropertyName("auto_send")]
    public required bool AutoSend { get; init; }

    /// <summary>The invoice details of the ebill - all four members are required once it is set; leaving it unset makes Pingen extract them from the PDF.</summary>
    [JsonPropertyName("meta_data")]
    public EbillMetaData? MetaData { get; init; }

    /// <summary>The preset the ebill inherits its defaults from.</summary>
    [JsonIgnore]
    public Guid? PresetId { get; init; }
}
