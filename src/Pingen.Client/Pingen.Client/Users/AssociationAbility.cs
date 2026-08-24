namespace Pingen.Client.Users;

/// <summary>
/// The ability names a membership reports in <c>meta.abilities</c>, each keyed to a state named on
/// <see cref="Common.JsonApi.AbilityState"/>.
/// The <c>manage</c> ability an association also reports arrives in a group this client does not surface.
/// </summary>
public static class AssociationAbility
{
    /// <summary>
    /// Block the membership.
    /// </summary>
    public const string Block = "block";

    /// <summary>
    /// Join the organisation.
    /// </summary>
    public const string Join = "join";

    /// <summary>
    /// Leave the organisation.
    /// </summary>
    public const string Leave = "leave";
}
