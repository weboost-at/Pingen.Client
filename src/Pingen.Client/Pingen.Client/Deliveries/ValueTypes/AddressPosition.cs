using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.ValueTypes;

/// <summary>
/// Which side of the first page carries the recipient address window.
/// </summary>
public enum AddressPosition
{
    /// <summary>
    /// The address sits in the left window.
    /// </summary>
    [JsonStringEnumMemberName("left")]
    Left,

    /// <summary>
    /// The address sits in the right window.
    /// </summary>
    [JsonStringEnumMemberName("right")]
    Right,
}
