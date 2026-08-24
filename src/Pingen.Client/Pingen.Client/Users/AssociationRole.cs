namespace Pingen.Client.Users;

/// <summary>
/// What a user may do in an organisation they belong to.
/// </summary>
public static class AssociationRole
{
    /// <summary>
    /// Full control, including billing and deletion.
    /// </summary>
    public const string Owner = "owner";

    /// <summary>
    /// Day-to-day use without ownership rights.
    /// </summary>
    public const string Manager = "manager";
}
