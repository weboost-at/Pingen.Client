namespace Pingen.Client.Deliveries.Letters;

/// <summary>
/// The statuses a letter has been observed to carry - Pingen publishes no complete list, and declares no
/// enum for any channel, so the letter, email and ebill vocabularies stay separate rather than asserting a
/// shared set.
/// </summary>
public static class LetterStatus
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
    /// Something needs a decision before the delivery can go on.
    /// </summary>
    public const string ActionRequired = "action_required";

    /// <summary>
    /// A correction is being applied to the file.
    /// </summary>
    public const string Fixing = "fixing";

    /// <summary>
    /// The delivery was submitted for dispatch.
    /// </summary>
    public const string Submitted = "submitted";

    /// <summary>
    /// Dispatch is waiting for the organisation's balance to cover it.
    /// </summary>
    public const string AwaitingCredits = "awaiting_credits";

    /// <summary>
    /// Pingen accepted the delivery for production.
    /// </summary>
    public const string Accepted = "accepted";

    /// <summary>
    /// The content is being inspected.
    /// </summary>
    public const string Inspection = "inspection";

    /// <summary>
    /// The delivery is being prepared for production.
    /// </summary>
    public const string Processing = "processing";

    /// <summary>
    /// The letter is being printed.
    /// </summary>
    public const string Printing = "printing";

    /// <summary>
    /// The letter is on its way to the distributor.
    /// </summary>
    public const string Transferring = "transferring";

    /// <summary>
    /// The delivery was handed to the distributor.
    /// </summary>
    public const string Sent = "sent";

    /// <summary>
    /// The delivery arrived.
    /// </summary>
    public const string Delivered = "delivered";

    /// <summary>
    /// The recipient could not be reached.
    /// </summary>
    public const string Undeliverable = "undeliverable";

    /// <summary>
    /// A print center rejected the letter.
    /// </summary>
    public const string Unprintable = "unprintable";

    /// <summary>
    /// Pingen rejected the delivery.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// The delivery expired before it was submitted.
    /// </summary>
    public const string Expired = "expired";

    /// <summary>
    /// A cancellation is being applied.
    /// </summary>
    public const string Cancelling = "cancelling";

    /// <summary>
    /// The delivery was cancelled.
    /// </summary>
    public const string Cancelled = "cancelled";

    /// <summary>
    /// The letter was cancelled after it expired.
    /// </summary>
    public const string CancelledExpired = "cancelled_expired";
}
