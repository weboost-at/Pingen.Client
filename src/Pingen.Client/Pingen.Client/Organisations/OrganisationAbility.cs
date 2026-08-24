namespace Pingen.Client.Organisations;

/// <summary>
/// The ability names an organisation reports in <c>meta.abilities</c>, each keyed to a state named on
/// <see cref="Common.JsonApi.AbilityState"/>.
/// </summary>
public static class OrganisationAbility
{
    /// <summary>
    /// Manage the organisation.
    /// </summary>
    public const string Manage = "manage";
}
