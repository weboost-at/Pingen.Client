using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>
/// A to-one relationship: the identity of the linked resource plus the link that fetches it.
/// </summary>
public record Relationship
{
    /// <summary>
    /// Identity of the linked resource.
    /// </summary>
    [JsonPropertyName("data")]
    public ResourceIdentifier? Data { get; init; }

    /// <summary>
    /// The link that fetches the related resource.
    /// </summary>
    [JsonPropertyName("links")]
    public RelationshipLinks? Links { get; init; }
}

/// <summary>
/// Identifies a resource by its id and JSON:API type.
/// </summary>
public record ResourceIdentifier
{
    /// <summary>
    /// The id of the resource.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, for example <c>letters</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }
}

/// <summary>
/// The links of a to-one relationship.
/// </summary>
public record RelationshipLinks
{
    /// <summary>
    /// The URL of the related resource.
    /// </summary>
    [JsonPropertyName("related")]
    public string? Related { get; init; }
}

/// <summary>
/// A to-many relationship, which Pingen exposes as a link and a count instead of embedded identities.
/// </summary>
public record RelatedCollection
{
    /// <summary>
    /// The link and count of the related collection.
    /// </summary>
    [JsonPropertyName("links")]
    public RelatedCollectionLinks? Links { get; init; }

    /// <summary>
    /// The URL that lists the related resources.
    /// </summary>
    [JsonIgnore]
    public string? Href => Links?.Related?.Href;

    /// <summary>
    /// The number of related resources, <c>0</c> when the API sent no count.
    /// </summary>
    [JsonIgnore]
    public int Count => Links?.Related?.Meta?.Count ?? 0;
}

/// <summary>
/// The links of a to-many relationship.
/// </summary>
public record RelatedCollectionLinks
{
    /// <summary>
    /// The link to the related collection.
    /// </summary>
    [JsonPropertyName("related")]
    public RelatedLink? Related { get; init; }
}

/// <summary>
/// A link to a related collection, carrying its own metadata.
/// </summary>
public record RelatedLink
{
    /// <summary>
    /// The URL that lists the related resources.
    /// </summary>
    [JsonPropertyName("href")]
    public required string Href { get; init; }

    /// <summary>
    /// The metadata of the related collection.
    /// </summary>
    [JsonPropertyName("meta")]
    public RelatedLinkMeta? Meta { get; init; }
}

/// <summary>
/// The metadata of a related collection.
/// </summary>
public record RelatedLinkMeta
{
    /// <summary>
    /// The number of related resources.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }
}
