namespace Pingen.Client.Webhooks.Payloads;

/// <summary>
/// Where a recipient's subscription to an ebill channel stands.
/// </summary>
public static class ChannelSubscriptionStatus
{
    /// <summary>
    /// The recipient receives ebills through the channel.
    /// </summary>
    public const string Active = "active";

    /// <summary>
    /// The subscription exists but is not delivering.
    /// </summary>
    public const string Inactive = "inactive";

    /// <summary>
    /// The recipient asked to subscribe and awaits approval.
    /// </summary>
    public const string Requested = "requested";
}
