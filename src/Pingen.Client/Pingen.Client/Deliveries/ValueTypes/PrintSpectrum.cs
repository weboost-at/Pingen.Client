using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.ValueTypes;

/// <summary>Which colors are printed.</summary>
public enum PrintSpectrum
{
    /// <summary>Full-color printing.</summary>
    [JsonStringEnumMemberName("color")]
    Color,

    /// <summary>Black and white printing.</summary>
    [JsonStringEnumMemberName("grayscale")]
    Grayscale,
}
