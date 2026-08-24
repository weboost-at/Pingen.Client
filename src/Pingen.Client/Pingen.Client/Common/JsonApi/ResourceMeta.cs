using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>
/// The metadata Pingen adds to single-resource responses - list items never carry it.
/// </summary>
public record ResourceMeta
{
    /// <summary>
    /// The ability groups as they arrive on the wire - <see cref="Abilities"/> exposes the <c>self</c> group, which
    /// is the only one every resource carries.
    /// </summary>
    [JsonPropertyName("abilities")]
    public ResourceAbilities? AbilityGroups { get; init; }

    /// <summary>
    /// What may be done with this resource, keyed by kebab-case ability name with the values named on
    /// <see cref="AbilityState"/>.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string>? Abilities => AbilityGroups?.Self;
}

/// <summary>
/// The ability groups of a single resource.
/// </summary>
public record ResourceAbilities
{
    /// <summary>
    /// Abilities on the resource itself - an association also carries an <c>organisation</c> group this record does
    /// not surface.
    /// </summary>
    [JsonPropertyName("self")]
    public IReadOnlyDictionary<string, string>? Self { get; init; }
}
