using System.Text.Json.Serialization;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Deliveries.Letters;

/// <summary>How a letter is dispatched when it is sent.</summary>
public record LetterSendOptions
{
    /// <summary>The product the letter is dispatched with.</summary>
    [JsonPropertyName("delivery_product")]
    public required DeliveryProduct DeliveryProduct { get; init; }

    /// <summary>Which sides of the paper are printed.</summary>
    [JsonPropertyName("print_mode")]
    public required PrintMode PrintMode { get; init; }

    /// <summary>Which colors are printed.</summary>
    [JsonPropertyName("print_spectrum")]
    public required PrintSpectrum PrintSpectrum { get; init; }

    /// <summary>The address blocks Pingen prints onto the letter instead of reading them from the PDF.</summary>
    [JsonPropertyName("meta_data")]
    public LetterMetaData? MetaData { get; init; }
}
