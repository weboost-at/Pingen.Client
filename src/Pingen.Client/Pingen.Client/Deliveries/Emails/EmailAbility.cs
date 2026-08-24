namespace Pingen.Client.Deliveries.Emails;

/// <summary>
/// The ability names an email delivery reports in <c>meta.abilities</c>, each keyed to a state named on
/// <see cref="Common.JsonApi.AbilityState"/>.
/// </summary>
public static class EmailAbility
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
    /// Delete it.
    /// </summary>
    public const string Delete = "delete";

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
}
