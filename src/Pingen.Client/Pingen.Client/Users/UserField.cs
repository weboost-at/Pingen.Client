namespace Pingen.Client.Users;

/// <summary>
/// The attribute names a user is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class UserField
{
    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.Email"/>.
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.FirstName"/>.
    /// </summary>
    public const string FirstName = "first_name";

    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.LastName"/>.
    /// </summary>
    public const string LastName = "last_name";

    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.Status"/>.
    /// </summary>
    public const string Status = "status";

    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.Language"/>.
    /// </summary>
    public const string Language = "language";

    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.Edition"/>.
    /// </summary>
    public const string Edition = "edition";

    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.Flags"/>.
    /// </summary>
    public const string Flags = "flags";

    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="UserAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
