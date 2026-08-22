using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.Emails;

/// <summary>
/// The envelope of an email delivery - the API requires all seven members whenever meta data is sent at all.
/// </summary>
public record EmailMetaData
{
    /// <summary>
    /// The display name the email is sent under - at most 255 characters.
    /// </summary>
    [JsonPropertyName("sender_name")]
    public required string SenderName { get; init; }

    /// <summary>
    /// The address the email is delivered to - at most 255 characters.
    /// </summary>
    [JsonPropertyName("recipient_email")]
    public required string RecipientEmail { get; init; }

    /// <summary>
    /// The display name of the recipient - at most 255 characters.
    /// </summary>
    [JsonPropertyName("recipient_name")]
    public required string RecipientName { get; init; }

    /// <summary>
    /// The address replies are directed to - at most 255 characters.
    /// </summary>
    [JsonPropertyName("reply_email")]
    public required string ReplyEmail { get; init; }

    /// <summary>
    /// The display name replies are directed to - at most 255 characters.
    /// </summary>
    [JsonPropertyName("reply_name")]
    public required string ReplyName { get; init; }

    /// <summary>
    /// The subject line - at most 255 characters.
    /// </summary>
    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    /// <summary>
    /// The body text accompanying the attached PDF - at most 16384 characters.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}
