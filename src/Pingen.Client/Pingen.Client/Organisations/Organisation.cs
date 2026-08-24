using System.Text.Json.Serialization;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Organisations;

/// <summary>
/// An organisation - the account every delivery, batch and webhook belongs to.
/// </summary>
public record Organisation
{
    /// <summary>
    /// The id of the organisation.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The JSON:API type of the resource, always <see cref="PingenType.Organisations"/>.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// The plan, billing state, defaults and limits of the organisation.
    /// </summary>
    [JsonPropertyName("attributes")]
    public required OrganisationAttributes Attributes { get; init; }

    /// <summary>
    /// The users associated with the organisation.
    /// </summary>
    [JsonPropertyName("relationships")]
    public OrganisationRelationships? Relationships { get; init; }

    /// <summary>
    /// The canonical URL of the resource.
    /// </summary>
    [JsonPropertyName("links")]
    public ResourceLinks? Links { get; init; }

    /// <summary>
    /// What may be done with the organisation - single-resource responses only, null on list items.
    /// </summary>
    [JsonPropertyName("meta")]
    public ResourceMeta? Meta { get; init; }
}

/// <summary>
/// The attributes of an organisation.
/// </summary>
public record OrganisationAttributes
{
    /// <summary>
    /// The name of the organisation.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Where the organisation stands - the values are named on <see cref="OrganisationStatus"/>.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// The plan the organisation is on, for example <c>free</c>.
    /// </summary>
    [JsonPropertyName("plan")]
    public required string Plan { get; init; }

    /// <summary>
    /// How the organisation pays - the values are named on <see cref="OrganisationBillingMode"/>.
    /// </summary>
    [JsonPropertyName("billing_mode")]
    public required string BillingMode { get; init; }

    /// <summary>
    /// The currency the balance and every price are quoted in - the values are named on
    /// <see cref="OrganisationBillingCurrency"/>.
    /// </summary>
    [JsonPropertyName("billing_currency")]
    public required string BillingCurrency { get; init; }

    /// <summary>
    /// The credits currently on the account.
    /// </summary>
    [JsonPropertyName("billing_balance")]
    public required decimal BillingBalance { get; init; }

    /// <summary>
    /// The credits missing to dispatch everything that is waiting.
    /// </summary>
    [JsonPropertyName("missing_credits")]
    public required decimal MissingCredits { get; init; }

    /// <summary>
    /// The edition the organisation is running.
    /// </summary>
    [JsonPropertyName("edition")]
    public required string Edition { get; init; }

    /// <summary>
    /// The ISO 3166-1 alpha-2 country new deliveries default to.
    /// </summary>
    [JsonPropertyName("default_country")]
    public required string DefaultCountry { get; init; }

    /// <summary>
    /// Which window new letters default to - <c>left</c> or <c>right</c>.
    /// </summary>
    [JsonPropertyName("default_address_position")]
    public required string DefaultAddressPosition { get; init; }

    /// <summary>
    /// How many months addresses are kept.
    /// </summary>
    [JsonPropertyName("data_retention_addresses")]
    public required int DataRetentionAddresses { get; init; }

    /// <summary>
    /// How many months uploaded PDFs are kept.
    /// </summary>
    [JsonPropertyName("data_retention_pdf")]
    public required int DataRetentionPdf { get; init; }

    /// <summary>
    /// How many letters the organisation may send per month.
    /// </summary>
    [JsonPropertyName("limits_monthly_letters_count")]
    public required int LimitsMonthlyLettersCount { get; init; }

    /// <summary>
    /// How many ebills the organisation may send per month.
    /// </summary>
    [JsonPropertyName("limits_monthly_ebills_count")]
    public required int LimitsMonthlyEbillsCount { get; init; }

    /// <summary>
    /// How many emails the organisation may send per month.
    /// </summary>
    [JsonPropertyName("limits_monthly_emails_count")]
    public required int LimitsMonthlyEmailsCount { get; init; }

    /// <summary>
    /// The colour the organisation is marked with in the app, as a hex triplet.
    /// </summary>
    [JsonPropertyName("color")]
    public required string Color { get; init; }

    /// <summary>
    /// The feature flags the organisation is enrolled in.
    /// </summary>
    [JsonPropertyName("flags")]
    public required IReadOnlyList<string> Flags { get; init; }

    /// <summary>
    /// The instant the organisation was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The instant the organisation was last changed.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// The resources an organisation is related to.
/// </summary>
public record OrganisationRelationships
{
    /// <summary>
    /// The users associated with the organisation, exposed as a link and a count instead of embedded identities.
    /// </summary>
    [JsonPropertyName("associations")]
    public RelatedCollection? Associations { get; init; }
}
