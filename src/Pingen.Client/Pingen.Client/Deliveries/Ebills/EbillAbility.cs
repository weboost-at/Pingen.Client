namespace Pingen.Client.Deliveries.Ebills;

/// <summary>
/// The ability names an ebill delivery reports in <c>meta.abilities</c>, each keyed to a state named on
/// <see cref="Common.JsonApi.AbilityState"/>.
/// </summary>
public static class EbillAbility
{
    /// <summary>
    /// Attach another file to the delivery.
    /// </summary>
    public const string AddAttachment = "add-attachment";

    /// <summary>
    /// Apply a preset's defaults.
    /// </summary>
    public const string ApplyPreset = "apply-preset";

    /// <summary>
    /// Stop it before it is dispatched.
    /// </summary>
    public const string Cancel = "cancel";

    /// <summary>
    /// Save the current settings as a preset.
    /// </summary>
    public const string CreatePreset = "create-preset";

    /// <summary>
    /// Set the invoice date.
    /// </summary>
    public const string DefineInvoiceDate = "define-invoice-date";

    /// <summary>
    /// Set the invoice due date.
    /// </summary>
    public const string DefineInvoiceDueDate = "define-invoice-due-date";

    /// <summary>
    /// Set the invoice number.
    /// </summary>
    public const string DefineInvoiceNumber = "define-invoice-number";

    /// <summary>
    /// Set the QR payment part.
    /// </summary>
    public const string DefineQr = "define-qr";

    /// <summary>
    /// Set the recipient identifier.
    /// </summary>
    public const string DefineRecipientIdentifier = "define-recipient-identifier";

    /// <summary>
    /// Delete it.
    /// </summary>
    public const string Delete = "delete";

    /// <summary>
    /// Correct the page format.
    /// </summary>
    public const string FixFormat = "fix-format";

    /// <summary>
    /// Download the file as it was uploaded.
    /// </summary>
    public const string GetPdfRaw = "get-pdf-raw";

    /// <summary>
    /// Download the validation report.
    /// </summary>
    public const string GetPdfValidation = "get-pdf-validation";

    /// <summary>
    /// Undo the corrections and return to the uploaded file.
    /// </summary>
    public const string RestoreOriginal = "restore-original";

    /// <summary>
    /// Run validation again.
    /// </summary>
    public const string Revalidate = "revalidate";

    /// <summary>
    /// Hand it to production.
    /// </summary>
    public const string Submit = "submit";
}
