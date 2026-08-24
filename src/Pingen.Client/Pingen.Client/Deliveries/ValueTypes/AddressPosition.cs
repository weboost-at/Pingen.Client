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
    [JsonStringEnumMemberName(AddressPositionValue.Left)]
    Left,

    /// <summary>
    /// The address sits in the right window.
    /// </summary>
    [JsonStringEnumMemberName(AddressPositionValue.Right)]
    Right,
}

/// <summary>
/// The wire values <see cref="AddressPosition"/> serializes to, for comparing the strings responses carry back.
/// </summary>
public static class AddressPositionValue
{
    /// <summary>
    /// The address sits in the left window.
    /// </summary>
    public const string Left = "left";

    /// <summary>
    /// The address sits in the right window.
    /// </summary>
    public const string Right = "right";
}
