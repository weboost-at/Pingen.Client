namespace Pingen.Client.Webhooks;

/// <summary>
/// The attribute names a webhook is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class WebhookField
{
    /// <summary>
    /// Sorts and filters on <see cref="WebhookAttributes.EventCategory"/>.
    /// </summary>
    public const string EventCategory = "event_category";

    /// <summary>
    /// Sorts and filters on <see cref="WebhookAttributes.Url"/>.
    /// </summary>
    public const string Url = "url";

    /// <summary>
    /// Sorts and filters on <see cref="WebhookAttributes.SigningKey"/>.
    /// </summary>
    public const string SigningKey = "signing_key";
}
