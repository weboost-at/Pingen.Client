namespace Pingen.Client.Deliveries.Ebills;

/// <summary>
/// The statuses an ebill delivery has been observed to carry - Pingen publishes no complete list, and declares no
/// enum for any channel, so the letter, email and ebill vocabularies stay separate rather than asserting a
/// shared set.
/// </summary>
public static class EbillStatus
{
    /// <summary>
    /// The file is being validated.
    /// </summary>
    public const string Validating = "validating";

    /// <summary>
    /// The file passed validation and awaits submission.
    /// </summary>
    public const string Valid = "valid";

    /// <summary>
    /// The file failed validation.
    /// </summary>
    public const string Invalid = "invalid";

    /// <summary>
    /// Something needs a decision before the ebill can go on.
    /// </summary>
    public const string ActionRequired = "action_required";

    /// <summary>
    /// A correction is being applied to the file.
    /// </summary>
    public const string Fixing = "fixing";

    /// <summary>
    /// The ebill was submitted for dispatch.
    /// </summary>
    public const string Submitted = "submitted";

    /// <summary>
    /// Dispatch is waiting for the organisation's balance to cover it.
    /// </summary>
    public const string AwaitingCredits = "awaiting_credits";

    /// <summary>
    /// Pingen accepted the ebill for production.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The content is being inspected.
    /// </summary>
    public const string Inspection = "inspection";

    /// <summary>
    /// The ebill is being prepared for production.
    /// </summary>
    public const string Processing = "processing";

    /// <summary>
    /// The ebill was handed to the distributor.
    /// </summary>
    public const string Sent = "sent";

    /// <summary>
    /// The ebill arrived.
    /// </summary>
    public const string Delivered = "delivered";

    /// <summary>
    /// The recipient could not be reached.
    /// </summary>
    public const string Undeliverable = "undeliverable";

    /// <summary>
    /// Pingen rejected the ebill.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// The ebill expired before it was submitted.
    /// </summary>
    public const string Expired = "expired";

    /// <summary>
    /// A cancellation is being applied.
    /// </summary>
    public const string Cancelling = "cancelling";

    /// <summary>
    /// The ebill was cancelled.
    /// </summary>
    public const string Cancelled = "cancelled";
}
