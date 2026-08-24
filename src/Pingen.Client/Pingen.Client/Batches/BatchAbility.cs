namespace Pingen.Client.Batches;

/// <summary>
/// The ability names a batch reports in <c>meta.abilities</c>, each keyed to a state named on
/// <see cref="Common.JsonApi.AbilityState"/>.
/// </summary>
public static class BatchAbility
{
    /// <summary>
    /// Attach another file to the delivery.
    /// </summary>
    public const string AddAttachment = "add-attachment";

    /// <summary>
    /// Add deliveries to the batch.
    /// </summary>
    public const string AddDeliverables = "add-deliverables";

    /// <summary>
    /// Stop it before it is dispatched.
    /// </summary>
    public const string Cancel = "cancel";

    /// <summary>
    /// Move the recipient address to the other window.
    /// </summary>
    public const string ChangeWindowPosition = "change-window-position";

    /// <summary>
    /// Delete it.
    /// </summary>
    public const string Delete = "delete";

    /// <summary>
    /// Change its attributes.
    /// </summary>
    public const string Edit = "edit";

    /// <summary>
    /// Remove deliveries from the batch.
    /// </summary>
    public const string RemoveDeliverables = "remove-deliverables";

    /// <summary>
    /// Hand it to production.
    /// </summary>
    public const string Submit = "submit";
}
