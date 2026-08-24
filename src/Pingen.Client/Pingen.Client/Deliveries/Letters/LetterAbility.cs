namespace Pingen.Client.Deliveries.Letters;

/// <summary>
/// The ability names a letter reports in <c>meta.abilities</c>, each keyed to a state named on
/// <see cref="Common.JsonApi.AbilityState"/>.
/// </summary>
public static class LetterAbility
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
    /// Change which paper a page is printed on.
    /// </summary>
    public const string ChangePaperType = "change-paper-type";

    /// <summary>
    /// Move the recipient address to the other window.
    /// </summary>
    public const string ChangeWindowPosition = "change-window-position";

    /// <summary>
    /// Add a cover page carrying the address.
    /// </summary>
    public const string CreateCoverpage = "create-coverpage";

    /// <summary>
    /// Save the current settings as a preset.
    /// </summary>
    public const string CreatePreset = "create-preset";

    /// <summary>
    /// Delete it.
    /// </summary>
    public const string Delete = "delete";

    /// <summary>
    /// Change its attributes.
    /// </summary>
    public const string Edit = "edit";

    /// <summary>
    /// Correct the recipient address.
    /// </summary>
    public const string FixAddress = "fix-address";

    /// <summary>
    /// Correct which window the address shows through.
    /// </summary>
    public const string FixAddressPosition = "fix-address-position";

    /// <summary>
    /// Correct the page format.
    /// </summary>
    public const string FixFormat = "fix-format";

    /// <summary>
    /// Flatten interactive content the print centers cannot handle.
    /// </summary>
    public const string FixInteractiveContent = "fix-interactive-content";

    /// <summary>
    /// Overwrite content sitting in a restricted area.
    /// </summary>
    public const string FixOverwriteRestrictedAreas = "fix-overwrite-restricted-areas";

    /// <summary>
    /// Fall back to regular paper.
    /// </summary>
    public const string FixRegularPaper = "fix-regular-paper";

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
    /// Dispatch one-sided despite the request.
    /// </summary>
    public const string SendSimplex = "send-simplex";

    /// <summary>
    /// Hand it to production.
    /// </summary>
    public const string Submit = "submit";
}
