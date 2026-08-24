namespace Pingen.Client.Users;

/// <summary>
/// Where a user's membership in an organisation stands.
/// </summary>
public static class AssociationStatus
{
    /// <summary>
    /// The invitation has not been accepted yet.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// The membership is in effect.
    /// </summary>
    public const string Active = "active";

    /// <summary>
    /// The membership was blocked by an owner.
    /// </summary>
    public const string Blocked = "blocked";
}
