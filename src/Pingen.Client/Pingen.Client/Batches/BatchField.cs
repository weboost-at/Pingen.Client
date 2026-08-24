namespace Pingen.Client.Batches;

/// <summary>
/// The attribute names a batch is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class BatchField
{
    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.Name"/>.
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.ChannelType"/>.
    /// </summary>
    public const string ChannelType = "channel_type";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.Icon"/>.
    /// </summary>
    public const string Icon = "icon";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.Status"/>.
    /// </summary>
    public const string Status = "status";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.FileOriginalName"/>.
    /// </summary>
    public const string FileOriginalName = "file_original_name";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.LetterCount"/>.
    /// </summary>
    public const string LetterCount = "letter_count";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.DeliverableCount"/>.
    /// </summary>
    public const string DeliverableCount = "deliverable_count";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.AddressPosition"/>.
    /// </summary>
    public const string AddressPosition = "address_position";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.PrintMode"/>.
    /// </summary>
    public const string PrintMode = "print_mode";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.PrintSpectrum"/>.
    /// </summary>
    public const string PrintSpectrum = "print_spectrum";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.PriceCurrency"/>.
    /// </summary>
    public const string PriceCurrency = "price_currency";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.PriceValue"/>.
    /// </summary>
    public const string PriceValue = "price_value";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.Source"/>.
    /// </summary>
    public const string Source = "source";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.SubmittedAt"/>.
    /// </summary>
    public const string SubmittedAt = "submitted_at";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="BatchAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
