using System.Text.Json.Serialization;

namespace Pingen.Client.Common.Json;

/// <summary>Maps enums to their wire names through <see cref="JsonStringEnumMemberNameAttribute"/> on each member - Pingen mixes snake_case and kebab-case, so no naming policy can stand in for it.</summary>
public class PingenEnumConverter() : JsonStringEnumConverter(
    namingPolicy: null,
    allowIntegerValues: false
);
