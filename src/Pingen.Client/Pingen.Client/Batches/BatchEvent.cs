using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Batches;

/// <summary>
/// An event on a batch - unlike a delivery event it never carries an image, so batches have no event-image endpoint.
/// </summary>
public record BatchEvent
{
    /// <summary>
    /// The id of the event.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <c>batches_events</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// What happened, when and where.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required BatchEventAttributes Attributes { get; init; }

    /// <summary>
    /// The batch the event belongs to.
    /// </summary>
    [JsonPropertyName("relationships")]
    public BatchEventRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the event - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of a batch event.
/// </summary>
public record BatchEventAttributes
{
    /// <summary>
    /// The machine-readable event code - an open set Pingen deliberately does not enumerate.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// The human-readable event name, localized into the language the list was requested in.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Who emitted the event, for example <c>Pingen</c>.
    /// </summary>
    [JsonPropertyName("producer")]
    public required string Producer { get; init; }

    /// <summary>
    /// Where the event was emitted, for example <c>8051 Zürich, CH</c>.
    /// </summary>
    [JsonPropertyName("location")]
    public required string Location { get; init; }

    /// <summary>
    /// Additional values the producer attached to the event.
    /// </summary>
    [JsonPropertyName("data")]
    public required IReadOnlyList<string> Data { get; init; }

    /// <summary>
    /// The instant the event happened.
    /// </summary>
    [JsonPropertyName("emitted_at")]
    public required DateTimeOffset EmittedAt { get; init; }

    /// <summary>
    /// The instant the event was recorded.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The instant the event was last changed.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The resources a batch event is related to.
/// </summary>
public record BatchEventRelationships
{
    /// <summary>
    /// The batch the event belongs to.
    /// </summary>
    [JsonPropertyName("batch")]
    public Relationship? Batch { get; init; }
}
