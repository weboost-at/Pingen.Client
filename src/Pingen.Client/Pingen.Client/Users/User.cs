using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Users;

/// <summary>
/// The user the access token was issued for - a singleton resource, which is why it is addressed without an id.
/// </summary>
public record User
{
    /// <summary>
    /// The id of the user.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <c>users</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The name, email, language and state of the user.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required UserAttributes Attributes { get; init; }

    /// <summary>
    /// The associations and notifications of the user.
    /// </summary>
    [JsonPropertyName("relationships")]
    public UserRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the user - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of a user.
/// </summary>
public record UserAttributes
{
    /// <summary>
    /// The email address the user signs in with.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// The first name of the user.
    /// </summary>
    [JsonPropertyName("first_name")]
    public required string FirstName { get; init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [JsonPropertyName("last_name")]
    public required string LastName { get; init; }

    /// <summary>
    /// Where the user stands - observed values are <c>active</c>, <c>registered</c>, <c>invited</c>,
    /// <c>pending_deletion</c>, <c>unconfirmed</c> and <c>unconfirmed_expired</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// The language the user reads Pingen in, for example <c>en-GB</c> or <c>de-DE</c>.
    /// </summary>
    [JsonPropertyName("language")]
    public required string Language { get; init; }

    /// <summary>
    /// The edition the user is running.
    /// </summary>
    [JsonPropertyName("edition")]
    public required string Edition { get; init; }

    /// <summary>
    /// The feature flags the user is enrolled in.
    /// </summary>
    [JsonPropertyName("flags")]
    public required IReadOnlyList<string> Flags { get; init; }

    /// <summary>
    /// The instant the user was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The instant the user was last changed.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The resources a user is related to.
/// </summary>
public record UserRelationships
{
    /// <summary>
    /// The organisations the user is associated with, exposed as a link and a count instead of embedded identities.
    /// </summary>
    [JsonPropertyName("associations")]
    public RelatedCollection? Associations { get; init; }

    /// <summary>
    /// The notifications of the user, exposed as a link and a count instead of embedded identities.
    /// </summary>
    [JsonPropertyName("notifications")]
    public RelatedCollection? Notifications { get; init; }
}
