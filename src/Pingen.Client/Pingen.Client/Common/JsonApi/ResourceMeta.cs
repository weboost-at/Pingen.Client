using System.Text.Json.Serialization;

namespace Pingen.Client.Common.JsonApi;

/// <summary>The metadata Pingen adds to single-resource responses - list items never carry it.</summary>
public record ResourceMeta
{
    /// <summary>The ability groups as they arrive on the wire - <see cref="Abilities"/> exposes the only group Pingen fills.</summary>
    [JsonPropertyName("abilities")]
    public ResourceAbilities? AbilityGroups { get; init; }

    /// <summary>What may be done with this resource, keyed by kebab-case ability name with values <c>ok</c>, <c>state</c> or <c>permission</c>.</summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string>? Abilities => AbilityGroups?.Self;
}

/// <summary>The ability groups of a single resource.</summary>
public record ResourceAbilities
{
    /// <summary>Abilities on the resource itself.</summary>
    [JsonPropertyName("self")]
    public IReadOnlyDictionary<string, string>? Self { get; init; }
}
