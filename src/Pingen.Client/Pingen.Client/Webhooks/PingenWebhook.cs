using System.Buffers;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pingen.Client.Common;
using Pingen.Client.Common.Json;
using Pingen.Client.Common.JsonApi;
using Pingen.Client.Webhooks.Payloads;

namespace Pingen.Client.Webhooks;

/// <summary>Verifies and parses the payloads Pingen posts to a subscribed webhook URL.</summary>
public static class PingenWebhook
{
    /// <summary>The header the payload signature arrives in, a lowercase hex HMAC-SHA256 of the raw request body.</summary>
    public const string SignatureHeader = "Signature";

    private const string IssuesType = "webhook_issues";

    private const string SentType = "webhook_sent";

    private const string DeliveredType = "webhook_delivered";

    private const string UndeliverableType = "webhook_undeliverable";

    private const string ChannelSubscriptionsType = "webhook_channel_subscriptions";

    /// <summary>Verifies the signature of <paramref name="payload"/> and parses it into the event its <c>data.type</c> names, throwing a <see cref="PingenException"/> when the signature does not match or the type is unknown.</summary>
    public static WebhookEvent ConstructEvent(string payload, string signatureHeader, string signingKey) =>
        VerifySignature(payload, signatureHeader, signingKey)
            ? ParseEvent(payload)
            : throw Rejected("The signature does not match the payload - it was not signed with this webhook's key or the body was altered on the way.");

    /// <summary>Parses a payload whose origin is already established, without verifying its signature.</summary>
    public static WebhookEvent ParseEvent(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var data = document.RootElement.GetProperty("data");
        var type = data.GetProperty("type").GetString() ?? string.Empty;

        WebhookEvent @event = type switch
        {
            IssuesType => ReadAttributes<WebhookIssueEvent>(data),
            SentType => ReadAttributes<WebhookSentEvent>(data),
            DeliveredType => ReadAttributes<WebhookDeliveredEvent>(data),
            UndeliverableType => ReadAttributes<WebhookUndeliverableEvent>(data),
            ChannelSubscriptionsType => ReadAttributes<WebhookChannelSubscriptionEvent>(data) with { ChannelEbill = Related(data, "channel_ebill") },
            _ => throw Rejected($"'{type}' is not a webhook event type this version of the SDK knows."),
        };

        // All four deliverable categories name the deliverable and the delivery event the payload was raised for.
        if (@event is WebhookDeliverableEvent deliverable)
            @event = deliverable with { Deliverable = Related(data, "deliverable"), Event = Related(data, "event") };

        return @event with
        {
            Id = data.GetProperty("id").GetGuid(),
            Type = type,
            Organisation = Related(data, "organisation"),
        };
    }

    /// <summary>Recomputes the HMAC-SHA256 of the payload with <paramref name="signingKey"/> and compares it with the signature the request arrived with.</summary>
    public static bool VerifySignature(string payload, string signatureHeader, string signingKey)
    {
        // Hashed over the bytes as they arrived - re-serializing the payload would change them and break every signature.
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), Encoding.UTF8.GetBytes(payload));
        Span<byte> signature = stackalloc byte[expected.Length];

        // Constant-time - a byte-by-byte comparison leaks how much of a forged signature was already right.
        return Convert.FromHexString(signatureHeader, signature, out _, out var written) is OperationStatus.Done
            && written == expected.Length
            && CryptographicOperations.FixedTimeEquals(signature, expected);
    }

    private static T ReadAttributes<T>(JsonElement data) where T : WebhookEvent =>
        data.GetProperty("attributes").Deserialize<T>(PingenJson.Options)!;

    private static Relationship? Related(JsonElement data, string name) =>
        data.TryGetProperty("relationships", out var relationships) && relationships.TryGetProperty(name, out var related)
            ? related.Deserialize<Relationship>(PingenJson.Options)
            : null;

    private static PingenException Rejected(string detail) =>
        new(
            statusCode: HttpStatusCode.BadRequest,
            errors: [new() { Title = "The webhook payload was rejected", Detail = detail }]
        );
}
