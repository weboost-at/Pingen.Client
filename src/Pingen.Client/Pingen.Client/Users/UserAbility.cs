namespace Pingen.Client.Users;

/// <summary>
/// The ability names a user reports in <c>meta.abilities</c>, each keyed to a state named on
/// <see cref="Common.JsonApi.AbilityState"/>.
/// </summary>
public static class UserAbility
{
    /// <summary>
    /// Act on behalf of the user.
    /// </summary>
    public const string Act = "act";

    /// <summary>
    /// Contact the user.
    /// </summary>
    public const string Reach = "reach";

    /// <summary>
    /// Send the activation mail again.
    /// </summary>
    public const string ResendActivation = "resend-activation";
}
