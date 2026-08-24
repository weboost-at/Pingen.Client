namespace Pingen.Client.Organisations;

/// <summary>
/// How an organisation pays for its deliveries.
/// </summary>
public static class OrganisationBillingMode
{
    /// <summary>
    /// Deliveries are drawn from a balance topped up in advance.
    /// </summary>
    public const string Prepaid = "prepaid";

    /// <summary>
    /// Deliveries are invoiced after the fact.
    /// </summary>
    public const string Postpaid = "postpaid";
}
