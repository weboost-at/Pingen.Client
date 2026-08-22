using System.Text.Json.Serialization;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Deliveries.Letters;

/// <summary>The shape of the letter a price is calculated for.</summary>
public record LetterPriceOptions
{
    /// <summary>The ISO 3166-1 alpha-2 country the letter would be delivered to.</summary>
    [JsonPropertyName("country")]
    public required string Country { get; init; }

    /// <summary>The kind of paper each page is printed on, one entry per page.</summary>
    [JsonPropertyName("paper_types")]
    public required IReadOnlyList<PaperType> PaperTypes { get; init; }

    /// <summary>Which sides of the paper are printed.</summary>
    [JsonPropertyName("print_mode")]
    public required PrintMode PrintMode { get; init; }

    /// <summary>Which colors are printed.</summary>
    [JsonPropertyName("print_spectrum")]
    public required PrintSpectrum PrintSpectrum { get; init; }

    /// <summary>The product the letter would be dispatched with.</summary>
    [JsonPropertyName("delivery_product")]
    public required DeliveryProduct DeliveryProduct { get; init; }
}

/// <summary>The kind of paper a page is printed on.</summary>
public enum PaperType
{
    /// <summary>Plain paper.</summary>
    [JsonStringEnumMemberName("normal")]
    Normal,

    /// <summary>A Swiss QR-bill payment part.</summary>
    [JsonStringEnumMemberName("qr")]
    Qr,

    /// <summary>An Austrian SEPA payment slip.</summary>
    [JsonStringEnumMemberName("sepa_at")]
    SepaAt,

    /// <summary>A German SEPA payment slip.</summary>
    [JsonStringEnumMemberName("sepa_de")]
    SepaDe,
}
