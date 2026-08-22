using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Files;

/// <summary>A presigned upload target - the URL the file content is written to and the signature the create call echoes back.</summary>
public record FileUpload
{
    /// <summary>The id of the upload target.</summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>The JSON:API type of the resource, always <c>file_uploads</c>.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>The presigned URL, its signature and its expiry.</summary>
    [JsonPropertyName("attributes")]
    public required FileUploadAttributes Attributes { get; init; }

    /// <summary>The canonical URL of the resource.</summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }
}

/// <summary>The attributes of a presigned upload target.</summary>
public record FileUploadAttributes
{
    /// <summary>The presigned URL the file content is written to - single-use and short-lived.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>The signature a create call sends as <c>file_url_signature</c>, copied verbatim.</summary>
    [JsonPropertyName("url_signature")]
    public required string UrlSignature { get; init; }

    /// <summary>The instant the presigned URL stops accepting the upload.</summary>
    [JsonPropertyName("expires_at")]
    public required DateTimeOffset ExpiresAt { get; init; }
}
