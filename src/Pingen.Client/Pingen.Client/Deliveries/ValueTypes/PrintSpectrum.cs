using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.ValueTypes;

/// <summary>
/// Which colors are printed.
/// </summary>
public enum PrintSpectrum
{
    /// <summary>
    /// Full-color printing.
    /// </summary>
    [JsonStringEnumMemberName(PrintSpectrumValue.Color)]
    Color,

    /// <summary>
    /// Black and white printing.
    /// </summary>
    [JsonStringEnumMemberName(PrintSpectrumValue.Grayscale)]
    Grayscale,
}

/// <summary>
/// The wire values <see cref="PrintSpectrum"/> serializes to, for comparing the strings responses carry back.
/// </summary>
public static class PrintSpectrumValue
{
    /// <summary>
    /// Full-color printing.
    /// </summary>
    public const string Color = "color";

    /// <summary>
    /// Black and white printing.
    /// </summary>
    public const string Grayscale = "grayscale";
}
