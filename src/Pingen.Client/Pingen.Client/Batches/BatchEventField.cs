namespace Pingen.Client.Batches;

/// <summary>
/// The attribute names an event on a batch is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class BatchEventField
{
    /// <summary>
    /// Sorts on the sequence Pingen orders events by - the default sort of every event list, and not an
    /// attribute the event carries.
    /// </summary>
    public const string RealId = "real_id";

    /// <summary>
    /// Sorts and filters on <see cref="BatchEventAttributes.Code"/>.
    /// </summary>
    public const string Code = "code";

    /// <summary>
    /// Sorts and filters on <see cref="BatchEventAttributes.Name"/>.
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// Sorts and filters on <see cref="BatchEventAttributes.Producer"/>.
    /// </summary>
    public const string Producer = "producer";

    /// <summary>
    /// Sorts and filters on <see cref="BatchEventAttributes.Location"/>.
    /// </summary>
    public const string Location = "location";

    /// <summary>
    /// Sorts and filters on <see cref="BatchEventAttributes.Data"/>.
    /// </summary>
    public const string Data = "data";

    /// <summary>
    /// Sorts and filters on <see cref="BatchEventAttributes.EmittedAt"/>.
    /// </summary>
    public const string EmittedAt = "emitted_at";

    /// <summary>
    /// Sorts and filters on <see cref="BatchEventAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="BatchEventAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
