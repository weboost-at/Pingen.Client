namespace Pingen.Client.Deliveries.Emails;

/// <summary>
/// The attribute names an email delivery is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class EmailField
{
    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.Status"/>.
    /// </summary>
    public const string Status = "status";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.FileOriginalName"/>.
    /// </summary>
    public const string FileOriginalName = "file_original_name";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.FilePages"/>.
    /// </summary>
    public const string FilePages = "file_pages";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.RecipientIdentifier"/>.
    /// </summary>
    public const string RecipientIdentifier = "recipient_identifier";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.PriceCurrency"/>.
    /// </summary>
    public const string PriceCurrency = "price_currency";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.PriceValue"/>.
    /// </summary>
    public const string PriceValue = "price_value";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.Source"/>.
    /// </summary>
    public const string Source = "source";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.SubmittedAt"/>.
    /// </summary>
    public const string SubmittedAt = "submitted_at";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="EmailAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
