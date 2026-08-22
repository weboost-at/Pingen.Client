using System.Text.Json.Serialization;
using Pingen.Client.Batches.ValueTypes;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Batches;

/// <summary>
/// What a batch is created with.
/// </summary>
public record BatchCreateOptions
{
    /// <summary>
    /// The name the batch is filed under - between 5 and 100 characters.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The icon the batch is shown with.
    /// </summary>
    [JsonPropertyName("icon")]
    public required BatchIcon Icon { get; init; }

    /// <summary>
    /// The file name the batch is filed under - between 5 and 255 characters.
    /// </summary>
    [JsonPropertyName("file_original_name")]
    public required string FileOriginalName { get; init; }

    /// <summary>
    /// The presigned URL the ZIP or merged PDF was written to - at most 1000 characters, filled by the overload taking
    /// a <see cref="Stream"/>.
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
    /// How the uploaded file carries the deliveries.
    /// </summary>
    [JsonPropertyName("grouping_type")]
    public required BatchGroupingType GroupingType { get; init; }

    /// <summary>
    /// What separates one delivery from the next inside the file.
    /// </summary>
    [JsonPropertyName("grouping_options_split_type")]
    public required BatchSplitType SplitType { get; init; }

    /// <summary>
    /// The channel the batch is dispatched through - default is <c>post</c>.
    /// </summary>
    [JsonPropertyName("channel_type")]
    public BatchChannelType? ChannelType { get; init; }

    /// <summary>
    /// Which window the recipient addresses show through - default is the organisation's setting.
    /// </summary>
    [JsonPropertyName("address_position")]
    public AddressPosition? AddressPosition { get; init; }

    /// <summary>
    /// How many pages each delivery has when <see cref="SplitType"/> is <see cref="BatchSplitType.Page"/> - between 1
    /// and 10.
    /// </summary>
    [JsonPropertyName("grouping_options_split_size")]
    public int? SplitSize { get; init; }

    /// <summary>
    /// The text marking a split when <see cref="SplitType"/> is <see cref="BatchSplitType.Custom"/> - at most 20
    /// characters.
    /// </summary>
    [JsonPropertyName("grouping_options_split_separator")]
    public string? SplitSeparator { get; init; }

    /// <summary>
    /// Where the separator sits within a delivery.
    /// </summary>
    [JsonPropertyName("grouping_options_split_position")]
    public BatchSplitPosition? SplitPosition { get; init; }

    /// <summary>
    /// The preset the batch inherits its defaults from - sent as the request's preset relationship, not as an
    /// attribute.
    /// </summary>
    [JsonIgnore]
    public Guid? PresetId { get; init; }
}

/// <summary>
/// The channel a batch is dispatched through.
/// </summary>
public enum BatchChannelType
{
    /// <summary>
    /// Physical mail.
    /// </summary>
    [JsonStringEnumMemberName("post")]
    Post,

    /// <summary>
    /// Electronic invoices.
    /// </summary>
    [JsonStringEnumMemberName("ebill")]
    Ebill,

    /// <summary>
    /// Electronic mail.
    /// </summary>
    [JsonStringEnumMemberName("email")]
    Email,
}

/// <summary>
/// How the file uploaded for a batch carries its deliveries.
/// </summary>
public enum BatchGroupingType
{
    /// <summary>
    /// An archive holding one file per delivery.
    /// </summary>
    [JsonStringEnumMemberName("zip")]
    Zip,

    /// <summary>
    /// A single PDF holding every delivery.
    /// </summary>
    [JsonStringEnumMemberName("merge")]
    Merge,
}

/// <summary>
/// What separates one delivery from the next inside the file uploaded for a batch.
/// </summary>
public enum BatchSplitType
{
    /// <summary>
    /// Every file of the archive is one delivery.
    /// </summary>
    [JsonStringEnumMemberName("file")]
    File,

    /// <summary>
    /// A fixed number of pages is one delivery.
    /// </summary>
    [JsonStringEnumMemberName("page")]
    Page,

    /// <summary>
    /// A page carrying the separator text starts a delivery.
    /// </summary>
    [JsonStringEnumMemberName("custom")]
    Custom,

    /// <summary>
    /// A page carrying a Swiss QR invoice code starts a delivery.
    /// </summary>
    [JsonStringEnumMemberName("qr_invoice")]
    QrInvoice,
}

/// <summary>
/// Where the page carrying the separator sits within a delivery.
/// </summary>
public enum BatchSplitPosition
{
    /// <summary>
    /// The separator opens the delivery.
    /// </summary>
    [JsonStringEnumMemberName("first_page")]
    FirstPage,

    /// <summary>
    /// The separator closes the delivery.
    /// </summary>
    [JsonStringEnumMemberName("last_page")]
    LastPage,
}
