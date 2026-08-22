# Pingen.Client

.NET client for the Pingen API — letters, emails, ebills, batches, organisations, users, webhooks and
file transfer. `net10.0`, nullable, DI-native, no framework coupling. By weboost.at, MIT licensed.

## Install

```shell
dotnet add package Pingen.Client
```

## Quickstart

Register the client once — the parameterless overload binds the `Pingen` configuration section and
validates it at startup:

```csharp
using Pingen.Client;

builder.Services.AddPingen();
```

```json
{
  "Pingen": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Environment": "Staging"
  }
}
```

Then inject `PingenClient` and reach the resources through its services:

```csharp
using Pingen.Client;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Deliveries.ValueTypes;

public class Mailer(PingenClient pingen)
{
    public Task<Letter> SendAsync(Guid organisationId, Stream pdf, CancellationToken cancellationToken) =>
        pingen.Letters.CreateAsync(organisationId, pdf, new LetterCreateOptions
        {
            FileOriginalName = "invoice.pdf",
            AutoSend = true,
            DeliveryProduct = DeliveryProduct.Cheap,
            PrintMode = PrintMode.Simplex,
            PrintSpectrum = PrintSpectrum.Grayscale,
        }, cancellationToken: cancellationToken);
}
```

That single call uploads the PDF and creates the letter. The eight services — `Letters`, `Emails`,
`Ebills`, `Batches`, `Organisations`, `Users`, `Webhooks`, `Files` — are also registered on their own,
so you can inject just `LetterService` instead of the whole client.

## Configuration

`PingenOptions` (namespace `Pingen.Client.Options`):

| Property | Type | Default | Meaning |
| --- | --- | --- | --- |
| `ClientId` | `string?` | `null` | OAuth client id. Required — startup fails without it. |
| `ClientSecret` | `string?` | `null` | OAuth client secret. Required — startup fails without it. |
| `Environment` | `PingenEnvironment` | `Production` | `Production` or `Staging`; selects both the API and the identity host. |
| `Scopes` | `string?` | `null` | Space-separated scopes to request. `null` asks for every scope the client is registered for. |
| `BaseAddress` | `Uri?` | `null` | Overrides the API host of `Environment`. |
| `IdentityAddress` | `Uri?` | `null` | Overrides the identity host issuing access tokens. |

Configure in code instead of through `IConfiguration` with the delegate overload:

```csharp
using Pingen.Client;
using Pingen.Client.Options;

builder.Services.AddPingen(options =>
{
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.Environment = PingenEnvironment.Staging;
    options.Scopes = "letter webhook user";
});
```

Access tokens are fetched with the OAuth `client_credentials` grant, cached for their 12-hour lifetime
and refreshed shortly before they expire — you never handle a token yourself.

**Scopes.** The documented set is `letter`, `ebill`, `email`, `batch`, `webhook` and
`organisation_read`. The `/user` endpoints behind `pingen.Users` additionally need the `user` scope,
which Pingen's published scope list omits — if you set `Scopes` explicitly and call `Users`, add
`user` to the string yourself.

## Concepts

**Organisation-scoped calls.** Everything except `pingen.Users` lives under an organisation, so the
first parameter of nearly every method is a `Guid organisationId`. Discover yours with
`pingen.Organisations.ListAsync()` or `pingen.Users.ListAssociationsAsync()`.

**JSON:API shape.** The API is JSON:API, and the resource records keep that shape: `Id`, `Type`,
`Attributes`, `Relationships`, `Links`, `Meta`. The payload you usually want sits one level down —
`letter.Attributes.Status`, `letter.Attributes.FileOriginalName`, `organisation.Attributes.Name`.
`Meta` is populated on single-resource responses only and is `null` on list items.

**Lists and auto-paging.** Every list call returns `PingenList<T>`, which is an `IReadOnlyList<T>`
carrying the `Links` and `Meta` the API sent (`Meta.CurrentPage`, `Meta.LastPage`, `Meta.Total`).
Each list method has a `…AutoPagingAsync` twin returning `IAsyncEnumerable<T>` that walks pages for you:

```csharp
await foreach (var letter in pingen.Letters.ListAutoPagingAsync(organisationId, cancellationToken: cancellationToken))
    Console.WriteLine($"{letter.Attributes.FileOriginalName}: {letter.Attributes.Status}");
```

**`PingenListOptions`.** Paging, sorting, filtering and shaping in one record: `PageNumber`,
`PageLimit` (max 100), `Sort` (`-` prefix for descending), `Filter`, `Search`, `Include`, `Language`
and `Fields` (sparse fieldsets, keyed by JSON:API type). Filters are built with `PingenFilter` —
`Where`, `Not`, `Contains`, `GreaterThan`, `GreaterOrEqual`, `LessThan`, `LessOrEqual`, combined with
`And` / `Or`, plus `Raw` as an escape hatch. The comparison factories also take `DateOnly` and
`DateTimeOffset`:

```csharp
using Pingen.Client.Common;

var options = new PingenListOptions
{
    PageLimit = 100,
    Sort = "-created_at",
    Search = "invoice",
    Fields = new Dictionary<string, string> { ["letters"] = "status,file_original_name" },
    Filter = PingenFilter.And(
        PingenFilter.Where("status", "sent"),
        PingenFilter.GreaterOrEqual("created_at", new DateOnly(2026, 1, 1))
    ),
};

var page = await pingen.Letters.ListAsync(organisationId, options, cancellationToken);
```

A few endpoints accept no sorting at all (the four organisation-wide letter event lists and the
webhook list); a `Sort` handed to them is dropped rather than rejected.

**Idempotency.** Every mutating method takes an optional `PingenRequestOptions`. Setting
`IdempotencyKey` makes Pingen replay the original response instead of repeating the operation for 24
hours. Keys are **never** generated for you — if you want one, pass one:

```csharp
await pingen.Letters.CreateAsync(
    organisationId,
    pdf,
    createOptions,
    new PingenRequestOptions { IdempotencyKey = Guid.NewGuid().ToString() },
    cancellationToken
);
```

**Errors.** Any non-success response throws `PingenException`, carrying `StatusCode`, the API's
`Errors` list (`Code`, `Title`, `Detail`, `Source`), the `RequestId` worth quoting to Pingen support,
and `RetryAfter`. The SDK does **not** auto-retry rate limits (429) or maintenance windows (503) —
they surface as exceptions with `RetryAfter` set, and the backoff policy is yours to choose:

```csharp
using System.Net;
using Pingen.Client.Common;

try
{
    await pingen.Letters.SendAsync(organisationId, letterId, sendOptions, cancellationToken: cancellationToken);
}
catch (PingenException exception) when (exception.StatusCode is HttpStatusCode.TooManyRequests)
{
    await Task.Delay(exception.RetryAfter ?? TimeSpan.FromSeconds(30), cancellationToken);
}
```

## Files

Pingen uploads run in three steps: request a presigned target, `PUT` the raw bytes to it, then create
the resource with the returned URL and signature. The `Stream` overloads of `CreateAsync` on
`Letters`, `Emails`, `Ebills` and `Batches` do all three for you — leave `FileUrl` and
`FileUrlSignature` unset and they are filled in from the upload.

Drive it manually when you want to reuse one upload or upload ahead of time:

```csharp
using Pingen.Client.Deliveries.Letters;

var upload = await pingen.Files.UploadAsync(pdf, cancellationToken);

await pingen.Letters.CreateAsync(organisationId, new LetterCreateOptions
{
    FileOriginalName = "invoice.pdf",
    AutoSend = false,
    FileUrl = upload.Attributes.Url,
    FileUrlSignature = upload.Attributes.UrlSignature,
}, cancellationToken: cancellationToken);
```

`Files.RequestUploadAsync()` and `Files.UploadAsync(target, content)` split those two steps if you
need the target before the bytes are ready. Targets are single-use and expire — `Attributes.ExpiresAt`
says when.

Downloads work the other way round: the file endpoints answer `302` with a presigned URL, which the
client reads instead of following. `DownloadFileAsync` resolves and fetches it in one call; the
`…LocationAsync` methods hand you the URL instead. Presigned URLs are unauthenticated — sending a
bearer token to one breaks its signature, which is why `Files.DownloadAsync` uses its own unauthenticated
HTTP client:

```csharp
await using var content = await pingen.Letters.DownloadFileAsync(organisationId, letterId, cancellationToken);

// Or resolve now, fetch later.
var location = await pingen.Letters.GetFileLocationAsync(organisationId, letterId, cancellationToken);
await using var same = await pingen.Files.DownloadAsync(location, cancellationToken);
```

The same pair exists for event images (`GetEventImageLocationAsync` / `DownloadEventImageAsync`), for
example the scan of an undeliverable envelope.

## Webhooks

Subscribe with `Webhooks.CreateAsync`. One webhook carries exactly one event category, and the signing
key is yours to pick and keep — there is no update endpoint, so changing a subscription means deleting
and recreating it:

```csharp
using Pingen.Client.Webhooks;

var webhook = await pingen.Webhooks.CreateAsync(organisationId, new WebhookCreateOptions
{
    EventCategory = WebhookEventCategory.Delivered,
    Url = "https://example.com/pingen/webhook",
    SigningKey = signingKey,
}, cancellationToken: cancellationToken);
```

Inbound payloads arrive with an HMAC-SHA256 of the **raw** body in the `Signature` header.
`PingenWebhook.ConstructEvent` verifies it in constant time and parses the payload into the concrete
event record, throwing `PingenException` on a bad signature or an unknown type. Read the raw body —
re-serializing it changes the bytes and breaks every signature:

```csharp
using Pingen.Client.Common;
using Pingen.Client.Webhooks;

app.MapPost("/pingen/webhook", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync();
    var signature = request.Headers[PingenWebhook.SignatureHeader].ToString();

    try
    {
        var @event = PingenWebhook.ConstructEvent(payload, signature, signingKey);

        // Delivery is at-least-once - deduplicate on @event.Id before acting on it.
        await handler.HandleAsync(@event);
    }
    catch (PingenException)
    {
        return Results.Unauthorized();
    }

    return Results.Ok();
});
```

`ConstructEvent` returns a `WebhookEvent` — switch on the concrete type (`WebhookSentEvent`,
`WebhookDeliveredEvent`, `WebhookUndeliverableEvent`, `WebhookIssueEvent`,
`WebhookChannelSubscriptionEvent`) to branch. `PingenWebhook.VerifySignature` and
`PingenWebhook.ParseEvent` are available separately when your framework already did one half.
Answer `2xx` to acknowledge; anything else makes Pingen retry at 1 m, 5 m, 10 m, 1 h, 2 h and 4 h
before giving up.

## Staging & simulation

Set `Environment` to `Staging` and both hosts switch to `api-staging.pingen.com` and
`identity-staging.pingen.com`; `BaseAddress` and `IdentityAddress` override them individually. Staging
credentials are separate from production ones.

Nothing is printed or delivered on staging. Outcomes are driven by the **file name** you send as
`FileOriginalName`:

| File name suffix | Channel | Simulated outcome |
| --- | --- | --- |
| `*_simulate_undeliverable.pdf` | letters | the letter comes back undeliverable |
| `*_simulate_unprintable.pdf` | letters | the letter cannot be printed |
| `*_simulate_cancellable.pdf` | letters, ebills | the delivery stays cancellable |
| `*_simulate_refused.pdf` | ebills | the recipient refuses the invoice |
| `*_simulate_approved.pdf` | ebills | the recipient approves the invoice |
| `*_simulate_paid.pdf` | ebills | the invoice is paid |

## License

MIT. See the Pingen API reference at <https://api.pingen.com/documentation> for the wire-level
documentation behind this client.
