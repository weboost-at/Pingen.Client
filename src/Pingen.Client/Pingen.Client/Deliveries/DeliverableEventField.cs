namespace Pingen.Client.Deliveries;

/// <summary>
/// The attribute names an event on a delivery is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class DeliverableEventField
{
    /// <summary>
    /// Sorts on the sequence Pingen orders events by - the default sort of every event list, and not an
    /// attribute the event carries.
    /// </summary>
    public const string RealId = "real_id";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.Code"/>.
    /// </summary>
    public const string Code = "code";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.Name"/>.
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.Producer"/>.
    /// </summary>
    public const string Producer = "producer";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.Location"/>.
    /// </summary>
    public const string Location = "location";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.HasImage"/>.
    /// </summary>
    public const string HasImage = "has_image";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.Data"/>.
    /// </summary>
    public const string Data = "data";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.EmittedAt"/>.
    /// </summary>
    public const string EmittedAt = "emitted_at";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="DeliverableEventAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
