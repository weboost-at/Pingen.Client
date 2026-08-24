namespace Pingen.Client.Organisations;

/// <summary>
/// Where an organisation stands in its lifecycle.
/// </summary>
public static class OrganisationStatus
{
    /// <summary>
    /// The organisation is in normal use.
    /// </summary>
    public const string Active = "active";

    /// <summary>
    /// Termination was confirmed and the organisation is winding down.
    /// </summary>
    public const string TerminationConfirmed = "termination_confirmed";

    /// <summary>
    /// The organisation is queued for deletion.
    /// </summary>
    public const string PendingDeletion = "pending_deletion";
}
