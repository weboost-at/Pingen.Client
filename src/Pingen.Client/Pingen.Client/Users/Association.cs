using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Users;

/// <summary>
/// The membership tying the user to one organisation.
/// </summary>
public record Association
{
    /// <summary>
    /// The id of the association.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <c>associations</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The role and state of the membership.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required AssociationAttributes Attributes { get; init; }

    /// <summary>
    /// The organisation and the user the membership ties together.
    /// </summary>
    [JsonPropertyName("relationships")]
    public AssociationRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the association - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of an association.
/// </summary>
public record AssociationAttributes
{
    /// <summary>
    /// What the user may do in the organisation - <c>owner</c> or <c>manager</c>.
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// Where the membership stands - <c>pending</c>, <c>active</c> or <c>blocked</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// The instant the association was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The instant the association was last changed.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The resources an association is related to.
/// </summary>
public record AssociationRelationships
{
    /// <summary>
    /// The organisation the user is a member of.
    /// </summary>
    [JsonPropertyName("organisation")]
    public Relationship? Organisation { get; init; }

    /// <summary>
    /// The user the membership belongs to.
    /// </summary>
    [JsonPropertyName("user")]
    public Relationship? User { get; init; }
}
