namespace Pingen.Client.Users;

/// <summary>
/// The attribute names a membership is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class AssociationField
{
    /// <summary>
    /// Sorts and filters on <see cref="AssociationAttributes.Role"/>.
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// Sorts and filters on <see cref="AssociationAttributes.Status"/>.
    /// </summary>
    public const string Status = "status";

    /// <summary>
    /// Sorts and filters on <see cref="AssociationAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="AssociationAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
