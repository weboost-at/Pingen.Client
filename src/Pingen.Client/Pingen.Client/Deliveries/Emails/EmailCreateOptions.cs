using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.Emails;

/// <summary>
/// What an email delivery is created from.
/// </summary>
public record EmailCreateOptions
{
    /// <summary>
    /// The name the uploaded file is recorded under - at most 255 characters.
    /// </summary>
    [JsonPropertyName("file_original_name")]
    public required string FileOriginalName { get; init; }

    /// <summary>
    /// The presigned URL the PDF was uploaded to - at most 1000 characters; the <c>Stream</c> overload of
    /// <c>CreateAsync</c> fills it from the upload it performs.
    /// </summary>
    [JsonPropertyName("file_url")]
    public string? FileUrl { get; init; }

    /// <summary>
    /// The signature of the presigned URL, copied verbatim - at most 60 characters; the <c>Stream</c> overload of
    /// <c>CreateAsync</c> fills it from the upload it performs.
    /// </summary>
    [JsonPropertyName("file_url_signature")]
    public string? FileUrlSignature { get; init; }

    /// <summary>
    /// Whether the email is dispatched as soon as it validates instead of waiting - emails have no send endpoint, so an
    /// email left at <c>false</c> is only ever sent through a batch.
    /// </summary>
    [JsonPropertyName("auto_send")]
    public required bool AutoSend { get; init; }

    /// <summary>
    /// The envelope of the email - all seven members are required once it is set.
    /// </summary>
    [JsonPropertyName("meta_data")]
    public EmailMetaData? MetaData { get; init; }

    /// <summary>
    /// The preset the email inherits its defaults from.
    /// </summary>
    [JsonIgnore]
    public Guid? PresetId { get; init; }
}
