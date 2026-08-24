namespace Pingen.Client.Common.JsonApi;

/// <summary>
/// Why an ability on a resource is or is not available.
/// </summary>
public static class AbilityState
{
    /// <summary>
    /// The action can be taken.
    /// </summary>
    public const string Ok = "ok";

    /// <summary>
    /// The action does not apply while the resource is in its current state.
    /// </summary>
    public const string State = "state";

    /// <summary>
    /// The action is out of reach for the authenticated user.
    /// </summary>
    public const string Permission = "permission";
}
