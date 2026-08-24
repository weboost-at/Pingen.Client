namespace Pingen.Client.Organisations;

/// <summary>
/// The attribute names an organisation is sorted, filtered and shaped by - Pingen documents
/// no closed set, so these are the names its own model carries.
/// </summary>
public static class OrganisationField
{
    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.Name"/>.
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.Status"/>.
    /// </summary>
    public const string Status = "status";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.Plan"/>.
    /// </summary>
    public const string Plan = "plan";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.BillingMode"/>.
    /// </summary>
    public const string BillingMode = "billing_mode";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.BillingCurrency"/>.
    /// </summary>
    public const string BillingCurrency = "billing_currency";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.BillingBalance"/>.
    /// </summary>
    public const string BillingBalance = "billing_balance";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.MissingCredits"/>.
    /// </summary>
    public const string MissingCredits = "missing_credits";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.Edition"/>.
    /// </summary>
    public const string Edition = "edition";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.DefaultCountry"/>.
    /// </summary>
    public const string DefaultCountry = "default_country";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.DefaultAddressPosition"/>.
    /// </summary>
    public const string DefaultAddressPosition = "default_address_position";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.DataRetentionAddresses"/>.
    /// </summary>
    public const string DataRetentionAddresses = "data_retention_addresses";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.DataRetentionPdf"/>.
    /// </summary>
    public const string DataRetentionPdf = "data_retention_pdf";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.LimitsMonthlyLettersCount"/>.
    /// </summary>
    public const string LimitsMonthlyLettersCount = "limits_monthly_letters_count";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.LimitsMonthlyEbillsCount"/>.
    /// </summary>
    public const string LimitsMonthlyEbillsCount = "limits_monthly_ebills_count";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.LimitsMonthlyEmailsCount"/>.
    /// </summary>
    public const string LimitsMonthlyEmailsCount = "limits_monthly_emails_count";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.Color"/>.
    /// </summary>
    public const string Color = "color";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.Flags"/>.
    /// </summary>
    public const string Flags = "flags";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.CreatedAt"/>.
    /// </summary>
    public const string CreatedAt = "created_at";

    /// <summary>
    /// Sorts and filters on <see cref="OrganisationAttributes.UpdatedAt"/>.
    /// </summary>
    public const string UpdatedAt = "updated_at";
}
