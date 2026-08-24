namespace Pingen.Client;

/// <summary>
/// The JSON:API resource types this client writes and matches - Pingen declares many more, so an unrecognised type is
/// possible on any relationship.
/// </summary>
public static class PingenType
{
    /// <summary>
    /// A letter.
    /// </summary>
    public const string Letters = "letters";

    /// <summary>
    /// An email delivery.
    /// </summary>
    public const string Emails = "emails";

    /// <summary>
    /// An ebill delivery.
    /// </summary>
    public const string Ebills = "ebills";

    /// <summary>
    /// A batch.
    /// </summary>
    public const string Batches = "batches";

    /// <summary>
    /// An organisation.
    /// </summary>
    public const string Organisations = "organisations";

    /// <summary>
    /// A user.
    /// </summary>
    public const string Users = "users";

    /// <summary>
    /// A membership of a user in an organisation.
    /// </summary>
    public const string Associations = "associations";

    /// <summary>
    /// A webhook subscription.
    /// </summary>
    public const string Webhooks = "webhooks";

    /// <summary>
    /// A preset a delivery inherits its defaults from.
    /// </summary>
    public const string Presets = "presets";

    /// <summary>
    /// A presigned file upload.
    /// </summary>
    public const string FileUploads = "file_uploads";

    /// <summary>
    /// The request and result of the letter price calculator.
    /// </summary>
    public const string LetterPriceCalculator = "letter_price_calculator";

    /// <summary>
    /// An event on a letter.
    /// </summary>
    public const string LettersEvents = "letters_events";

    /// <summary>
    /// An event on an email or ebill delivery.
    /// </summary>
    public const string DeliverablesEvents = "deliverables_events";

    /// <summary>
    /// An event on a batch.
    /// </summary>
    public const string BatchesEvents = "batches_events";

    /// <summary>
    /// The statistics of a batch.
    /// </summary>
    public const string BatchDetailsStatistics = "batch_details_statistics";

    /// <summary>
    /// An ebill channel a subscription belongs to.
    /// </summary>
    public const string ChannelEbills = "channel_ebills";

    /// <summary>
    /// The request sending a batch as physical mail.
    /// </summary>
    public const string BatchesChannelPostSend = "batches_channel_post_send";

    /// <summary>
    /// The request sending a batch as email.
    /// </summary>
    public const string BatchesChannelEmailSend = "batches_channel_email_send";

    /// <summary>
    /// The request sending a batch as ebills.
    /// </summary>
    public const string BatchesChannelEbillSend = "batches_channel_ebill_send";

    /// <summary>
    /// The webhook payload reporting an issue on a delivery.
    /// </summary>
    public const string WebhookIssues = "webhook_issues";

    /// <summary>
    /// The webhook payload reporting a delivery was handed to the distributor.
    /// </summary>
    public const string WebhookSent = "webhook_sent";

    /// <summary>
    /// The webhook payload reporting a delivery arrived.
    /// </summary>
    public const string WebhookDelivered = "webhook_delivered";

    /// <summary>
    /// The webhook payload reporting a delivery could not be delivered.
    /// </summary>
    public const string WebhookUndeliverable = "webhook_undeliverable";

    /// <summary>
    /// The webhook payload reporting a change to an ebill channel subscription.
    /// </summary>
    public const string WebhookChannelSubscriptions = "webhook_channel_subscriptions";
}
