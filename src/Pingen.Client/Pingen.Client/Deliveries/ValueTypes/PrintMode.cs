using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.ValueTypes;

/// <summary>
/// Which sides of the paper are printed.
/// </summary>
public enum PrintMode
{
    /// <summary>
    /// One-sided printing.
    /// </summary>
    [JsonStringEnumMemberName("simplex")]
    Simplex,

    /// <summary>
    /// Two-sided printing.
    /// </summary>
    [JsonStringEnumMemberName("duplex")]
    Duplex,
}
