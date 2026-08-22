using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Deliveries;

/// <summary>
/// An event on a letter, an email or an ebill - the three channels share one shape and differ only in the relationship
/// naming their parent.
/// </summary>
public record DeliverableEvent
{
    /// <summary>
    /// The id of the event.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource - <c>letters_events</c> on letters, <c>deliverables_events</c> on emails and
    /// ebills.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// What happened, when and where.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required DeliverableEventAttributes Attributes { get; init; }

    /// <summary>
    /// The deliverable the event belongs to.
    /// </summary>
    [JsonPropertyName("relationships")]
    public DeliverableEventRelationships? Relationships { get; init; }

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
/// The attributes of a delivery event.
/// </summary>
public record DeliverableEventAttributes
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
    /// Who emitted the event, for example <c>Pingen</c> or the postal service.
    /// </summary>
    [JsonPropertyName("producer")]
    public required string Producer { get; init; }

    /// <summary>
    /// Where the event was emitted, for example <c>8051 Zürich, CH</c>.
    /// </summary>
    [JsonPropertyName("location")]
    public required string Location { get; init; }

    /// <summary>
    /// Whether an image of the event is available for download.
    /// </summary>
    [JsonPropertyName("has_image")]
    public required bool HasImage { get; init; }

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
/// The parent of a delivery event, which the API names after the channel the event arrived on.
/// </summary>
public record DeliverableEventRelationships
{
    /// <summary>
    /// The letter the event belongs to, sent on letter events only.
    /// </summary>
    [JsonPropertyName("letter")]
    public Relationship? Letter { get; init; }

    /// <summary>
    /// The email the event belongs to, sent on email events only.
    /// </summary>
    [JsonPropertyName("email")]
    public Relationship? Email { get; init; }

    /// <summary>
    /// The ebill the event belongs to, sent on ebill events only.
    /// </summary>
    [JsonPropertyName("ebill")]
    public Relationship? Ebill { get; init; }

    /// <summary>
    /// The deliverable the event belongs to, whichever channel it arrived on.
    /// </summary>
    [JsonIgnore]
    public Relationship? Parent => Letter ?? Email ?? Ebill;
}
