using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.ValueTypes;

/// <summary>The postal product a delivery is dispatched with.</summary>
public enum DeliveryProduct
{
    /// <summary>Priority mail.</summary>
    [JsonStringEnumMemberName("fast")]
    Fast,

    /// <summary>Economy mail.</summary>
    [JsonStringEnumMemberName("cheap")]
    Cheap,

    /// <summary>Bulk mail.</summary>
    [JsonStringEnumMemberName("bulk")]
    Bulk,

    /// <summary>Premium mail.</summary>
    [JsonStringEnumMemberName("premium")]
    Premium,

    /// <summary>Registered mail.</summary>
    [JsonStringEnumMemberName("registered")]
    Registered,
}
