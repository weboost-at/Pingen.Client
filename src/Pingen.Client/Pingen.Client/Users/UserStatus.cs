namespace Pingen.Client.Users;

/// <summary>
/// Where a user stands in the sign-up and account lifecycle.
/// </summary>
public static class UserStatus
{
    /// <summary>
    /// The account is confirmed and in use.
    /// </summary>
    public const string Active = "active";

    /// <summary>
    /// The account was registered and is awaiting its first sign-in.
    /// </summary>
    public const string Registered = "registered";

    /// <summary>
    /// The user was invited and has not accepted yet.
    /// </summary>
    public const string Invited = "invited";

    /// <summary>
    /// The account is queued for deletion.
    /// </summary>
    public const string PendingDeletion = "pending_deletion";

    /// <summary>
    /// The email address has not been confirmed.
    /// </summary>
    public const string Unconfirmed = "unconfirmed";

    /// <summary>
    /// The confirmation window elapsed unused.
    /// </summary>
    public const string UnconfirmedExpired = "unconfirmed_expired";
}
