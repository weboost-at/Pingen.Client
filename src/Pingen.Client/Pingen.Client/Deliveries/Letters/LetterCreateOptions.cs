using System.Text.Json.Serialization;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Deliveries.Letters;

/// <summary>
/// What a letter is created with.
/// </summary>
public record LetterCreateOptions
{
    /// <summary>
    /// The file name the letter is filed under - at most 255 characters.
    /// </summary>
    [JsonPropertyName("file_original_name")]
    public required string FileOriginalName { get; init; }

    /// <summary>
    /// The presigned URL the PDF was written to - at most 1000 characters, filled by the overload taking a
    /// <see cref="Stream"/>.
    /// </summary>
    [JsonPropertyName("file_url")]
    public string? FileUrl { get; init; }

    /// <summary>
    /// The signature of the presigned URL - at most 60 characters, filled by the overload taking a
    /// <see cref="Stream"/>.
    /// </summary>
    [JsonPropertyName("file_url_signature")]
    public string? FileUrlSignature { get; init; }

    /// <summary>
    /// Whether Pingen submits the letter as soon as it validates instead of waiting for a send call.
    /// </summary>
    [JsonPropertyName("auto_send")]
    public required bool AutoSend { get; init; }

    /// <summary>
    /// Which window the recipient address shows through - default is the organisation's setting.
    /// </summary>
    [JsonPropertyName("address_position")]
    public AddressPosition? AddressPosition { get; init; }

    /// <summary>
    /// The product the letter is dispatched with - required when <see cref="AutoSend"/> is <c>true</c>.
    /// </summary>
    [JsonPropertyName("delivery_product")]
    public DeliveryProduct? DeliveryProduct { get; init; }

    /// <summary>
    /// Which sides of the paper are printed - required when <see cref="AutoSend"/> is <c>true</c>.
    /// </summary>
    [JsonPropertyName("print_mode")]
    public PrintMode? PrintMode { get; init; }

    /// <summary>
    /// Which colors are printed - required when <see cref="AutoSend"/> is <c>true</c>.
    /// </summary>
    [JsonPropertyName("print_spectrum")]
    public PrintSpectrum? PrintSpectrum { get; init; }

    /// <summary>
    /// The address blocks Pingen prints onto the letter instead of reading them from the PDF.
    /// </summary>
    [JsonPropertyName("meta_data")]
    public LetterMetaData? MetaData { get; init; }

    /// <summary>
    /// The preset the letter inherits its defaults from - sent as the request's preset relationship, not as an
    /// attribute.
    /// </summary>
    [JsonIgnore]
    public Guid? PresetId { get; init; }
}
