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
    [JsonStringEnumMemberName(PrintModeValue.Simplex)]
    Simplex,

    /// <summary>
    /// Two-sided printing.
    /// </summary>
    [JsonStringEnumMemberName(PrintModeValue.Duplex)]
    Duplex,
}

/// <summary>
/// The wire values <see cref="PrintMode"/> serializes to, for comparing the strings responses carry back.
/// </summary>
public static class PrintModeValue
{
    /// <summary>
    /// One-sided printing.
    /// </summary>
    public const string Simplex = "simplex";

    /// <summary>
    /// Two-sided printing.
    /// </summary>
    public const string Duplex = "duplex";
}
