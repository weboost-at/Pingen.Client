namespace Pingen.Client.Deliveries.Ebills;

/// <summary>
/// The attribute names an ebill delivery is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class EbillField
{
    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.Status"/>.
    /// </summary>
    public const string Status = "status";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.FileOriginalName"/>.
    /// </summary>
    public const string FileOriginalName = "file_original_name";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.FilePages"/>.
    /// </summary>
    public const string FilePages = "file_pages";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.RecipientIdentifier"/>.
    /// </summary>
    public const string RecipientIdentifier = "recipient_identifier";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.RecipientAddress"/>.
    /// </summary>
    public const string RecipientAddress = "recipient_address";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.InvoiceNumber"/>.
    /// </summary>
    public const string InvoiceNumber = "invoice_number";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.InvoiceDate"/>.
    /// </summary>
    public const string InvoiceDate = "invoice_date";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.InvoiceDueDate"/>.
    /// </summary>
    public const string InvoiceDueDate = "invoice_due_date";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.InvoiceValue"/>.
    /// </summary>
    public const string InvoiceValue = "invoice_value";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.InvoiceCurrency"/>.
    /// </summary>
    public const string InvoiceCurrency = "invoice_currency";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.InvoiceIban"/>.
    /// </summary>
    public const string InvoiceIban = "invoice_iban";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.InvoiceAddress"/>.
    /// </summary>
    public const string InvoiceAddress = "invoice_address";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.InvoiceReference"/>.
    /// </summary>
    public const string InvoiceReference = "invoice_reference";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.PriceCurrency"/>.
    /// </summary>
    public const string PriceCurrency = "price_currency";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.PriceValue"/>.
    /// </summary>
    public const string PriceValue = "price_value";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.Source"/>.
    /// </summary>
    public const string Source = "source";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.SubmittedAt"/>.
    /// </summary>
    public const string SubmittedAt = "submitted_at";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="EbillAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
