namespace Pingen.Client.Deliveries.Letters;

/// <summary>
/// The attribute names a letter is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class LetterField
{
    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.Status"/>.
    /// </summary>
    public const string Status = "status";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.FileOriginalName"/>.
    /// </summary>
    public const string FileOriginalName = "file_original_name";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.FilePages"/>.
    /// </summary>
    public const string FilePages = "file_pages";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.Address"/>.
    /// </summary>
    public const string Address = "address";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.AddressPosition"/>.
    /// </summary>
    public const string AddressPosition = "address_position";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.Country"/>.
    /// </summary>
    public const string Country = "country";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.DeliveryProduct"/>.
    /// </summary>
    public const string DeliveryProduct = "delivery_product";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.PrintMode"/>.
    /// </summary>
    public const string PrintMode = "print_mode";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.PrintSpectrum"/>.
    /// </summary>
    public const string PrintSpectrum = "print_spectrum";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.PriceCurrency"/>.
    /// </summary>
    public const string PriceCurrency = "price_currency";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.PriceValue"/>.
    /// </summary>
    public const string PriceValue = "price_value";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.PaperTypes"/>.
    /// </summary>
    public const string PaperTypes = "paper_types";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.Fonts"/>.
    /// </summary>
    public const string Fonts = "fonts";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.Source"/>.
    /// </summary>
    public const string Source = "source";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.TrackingNumber"/>.
    /// </summary>
    public const string TrackingNumber = "tracking_number";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.SubmittedAt"/>.
    /// </summary>
    public const string SubmittedAt = "submitted_at";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="LetterAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
