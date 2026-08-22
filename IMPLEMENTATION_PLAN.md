# Pingen.Client — Implementation Plan

A feature-complete, framework-agnostic .NET client for the [Pingen v2 API](https://api.pingen.com/documentation),
built DI-native on `Microsoft.Extensions.*`, with Stripe.net-inspired ergonomics, written exactly in the
house style of the `outreach` project (lead dev: Maximilian Hollnbuchner). This plan is the single source of
truth for the implementing agent. It was produced from the official OpenAPI spec (`.tmp/swagger-docs.json`,
gitignored) and a deep style audit of the outreach codebase — you should rarely need either original source.

**Read §1 (orchestration protocol) before doing anything else. You are an orchestrator, not a typist.**

---

## Table of contents

- §1 Orchestration & context-budget protocol
- §2 Repository state & ground rules
- §3 Architecture overview
- §4 API wire conventions (what every sub-agent must respect)
- §5 Project layout (files & folders)
- §6 Public API surface (types & method signatures)
- §7 Endpoint coverage matrix (all 49 operations — the definition of "feature complete")
- §8 Resource model reference (wire fields per resource)
- §9 Implementation phases & sub-agent briefs
- §10 Testing strategy
- §11 README requirements
- §12 House style digest & commit conventions
- §13 Decision log
- Appendix A: CLAUDE.md content to create in phase 0
- Appendix B: Spec extraction snippet (escape hatch)

---

## §1 Orchestration & context-budget protocol

Your context window is the scarcest resource in this task. The output degrades sharply if you run
low near the end, so operate under these hard rules:

1. **You never write production code yourself.** Every phase in §9 is executed by sub-agents
   (Agent tool / Task tool with `general-purpose` type, or this harness's equivalent). You only:
   read this plan once, dispatch briefs, run build/test gates, review summaries, commit, and keep tally.
2. **Sub-agents read; you don't.** Each brief tells the sub-agent to read the sections of this plan
   it needs (`.tmp/IMPLEMENTATION_PLAN.md` §N) plus `CLAUDE.md` — do NOT paste plan content into prompts.
   A brief should be ≤ 30 lines: phase goal, plan sections to read, files to create, definition of done.
3. **Never read generated source files back into your context.** Trust the build gate. After each
   phase run `dotnet build` and `dotnet test` on `src/Pingen.Client/Pingen.Client.sln` and read only
   the tail of failures. If a phase fails its gate, dispatch a *fix* sub-agent with the error output —
   do not debug inline.
4. **Never open `.tmp/swagger-docs.json` yourself** (5.5 MB / ~1.4M tokens). §4–§8 carry everything.
   If a sub-agent hits a genuine gap, it (not you) uses the python snippet in Appendix B to slice
   out exactly one schema or operation.
5. **Keep a running tally.** Maintain a scratch tally after every phase: phase done, gate result,
   ~% context consumed. If you cross ~60% consumed before phase 6, switch to terser briefs
   (section numbers only) and stop reading sub-agent reports beyond their first 10 lines.
6. **Sub-agent reports must be small.** Every brief ends with: "Report back ONLY: files created
   (paths), public types added (names only), gate status, deviations from the plan. Max 25 lines."
7. **Commit as you go — a requirement, not a preference.** One phase = one commit (occasionally
   two, see §9), committed and pushed immediately after the phase's gate passes — never batch
   work up for a big final commit. Every commit message follows §12.2 exactly: that is the repo
   owner's personal grammar and mimicking it is part of the task. Branching: if your session
   designates a branch, use it; otherwise either commit directly to `main` or run the ENTIRE
   implementation on one sensibly named branch in the house convention `feature/<topic>`
   (e.g. `feature/pingen-client`) — never per-phase branches, never throwaway names.
8. **Parallelism:** only the pairs 4+5 and 6+7 may run as parallel sub-agents (marked ⫲ in §9);
   their file lists are disjoint by design — `PingenClient.cs` hub properties and
   `PingenConfiguration.cs` service registrations belong to phase 8 alone, so no parallel phase
   ever edits a shared file. Do not parallelize anything else.

Recommended first actions, in order: read this file fully (once), execute phase 0 yourself (it is
mechanical file creation + package refs, listed exhaustively in §9), then dispatch phase 1.

---

## §2 Repository state & ground rules

What exists today (do not recreate, do not restructure):

```
Pingen.Client/                          # repo root
├── .github/workflows/ci.yml            # dotnet test on main/PRs — DO NOT TOUCH
├── .github/workflows/release.yml       # tag-driven NuGet publish — DO NOT TOUCH
├── .gitignore                          # includes .tmp/
├── .tmp/swagger-docs.json              # spec, gitignored, reference only
├── .tmp/IMPLEMENTATION_PLAN.md         # this file — gitignored working document, NEVER commit it
└── src/Pingen.Client/
    ├── Pingen.Client.sln
    ├── Directory.Build.props           # MinVer tag prefix v, IsPackable=false default
    ├── Pingen.Client/Pingen.Client.csproj        # net10.0, nullable, implicit usings,
    │                                             # packable, packs ../../../README.md
    └── Pingen.Client.Tests/Pingen.Client.Tests.csproj
```

Ground rules:

- Target stays **net10.0** only. "dotnet agnostic" means: no ASP.NET / hosting coupling; the library
  must work in any .NET app (console, worker, ASP.NET, Blazor). DI via
  `Microsoft.Extensions.DependencyInjection.Abstractions` + `Options` + `Http` only.
- **`dotnet pack` fails until `README.md` exists at the repo root** (the csproj packs it). The README
  is a first-class deliverable (§11); a placeholder may already exist uncommitted — replace its content.
- Never commit `.tmp/`. Never modify the workflows. Do not create a PR unless your session
  instructions say so. Push to the branch your session designates, `git push -u origin <branch>`;
  with no designated branch, commit to `main` directly or use a single `feature/<topic>` branch
  (house convention, e.g. `feature/pingen-client`) for the whole implementation.
- This plan is a working document that must NEVER enter `main`'s history: it lives at
  `.tmp/IMPLEMENTATION_PLAN.md` (gitignored, placed in the workspace before you start — if it is
  missing, fetch it from the disposable planning branch:
  `git fetch origin claude/happy-hypatia-a297k6 && git show FETCH_HEAD:IMPLEMENTATION_PLAN.md > .tmp/IMPLEMENTATION_PLAN.md`).
  Base all work on `main` — never branch from, merge, or cherry-pick the planning branch.
- Root namespace is `Pingen.Client`; namespace always equals folder path (`Pingen.Client.Batches`,
  `Pingen.Client.Deliveries.Letters`, …).
- Every public member gets a one-sentence XML `<summary>` (the whole library is consumed API —
  see §12). `GenerateDocumentationFile` is already on.

NuGet dependencies to add (phase 0) — keep this exact minimal set:

| Project | Package | Purpose |
|---|---|---|
| Pingen.Client | `Microsoft.Extensions.Http` | typed client + `IHttpClientFactory` + `DelegatingHandler` wiring |
| Pingen.Client | `Microsoft.Extensions.Options.ConfigurationExtensions` | `BindConfiguration("Pingen")` |
| Pingen.Client.Tests | `xunit.v3` (replace `xunit` 2.9.3) + matching `xunit.runner.visualstudio` | house test stack |
| Pingen.Client.Tests | `FluentAssertions` pinned `[7.2.0]` | house assertion style (pin exactly — license) |
| Pingen.Client.Tests | `Microsoft.Extensions.Configuration.Json` (only if needed for binding tests) | options binding test |

(`Microsoft.Extensions.Http` transitively brings DI abstractions, Options, Logging abstractions.
System.Text.Json is in-box on net10.0. No FluentValidation, no Polly, no Newtonsoft — see §13.)

---

## §3 Architecture overview

Stripe.net's developer experience, re-homed onto Maxi's DI-native idioms:

```
consumer code                      Pingen.Client internals
─────────────────                  ─────────────────────────────────────────────
services.AddPingen();       ─────► PingenConfiguration: options + validation,
                                   named/typed HttpClients, auth handler, services

PingenClient client (DI)    ─────► typed HttpClient wrapper; owns request building,
  .Letters / .Batches / …          serialization, error translation; exposes lazy
                                   per-resource services (Stripe "client.V1.X" hub)

LetterService (also DI-injectable directly)
  .CreateAsync(orgId, stream, options)  ──► 3-step upload orchestration
  .ListAsync(orgId, listOptions)        ──► PingenList<Letter> (+ auto-paging)
  .SendAsync / .CancelAsync / …

PingenAuthenticationHandler ─────► DelegatingHandler: attaches Bearer token from
                                   PingenAccessTokens (singleton, cached
                                   client_credentials token, 12h lifetime, one
                                   401-retry), talking to the identity host

PingenWebhook (static)      ─────► ConstructEvent(json, signature, signingKey):
                                   HMAC-SHA256 verify + typed payload parsing
```

Key structural decisions (rationale in §13):

- **Three HttpClients**: `PingenClient` (typed; API host; `AllowAutoRedirect = false`; auth handler),
  `"PingenIdentity"` (named; token endpoint; no auth handler), `"PingenFiles"` (named; presigned
  uploads/downloads; **no auth handler** — presigned URLs break if a Bearer header is sent).
- **Services are thin concrete classes** (primary constructor taking `PingenClient`), registered
  transient AND reachable via lazy properties on `PingenClient`. No interfaces (house style).
- **Responses are JSON:API-shaped records**: `letter.Attributes.Status`, `letter.Relationships.Batch`.
  Single-resource envelopes are unwrapped by the service (methods return `Letter`, not a document);
  list envelopes surface as `PingenList<T>` carrying `Meta`/`Links` and implementing `IReadOnlyList<T>`.
- **Requests are option records** (Stripe naming: `LetterCreateOptions`, `WebhookCreateOptions`, …)
  with `required` init properties for mandatory wire fields. One deliberate exemption: `FileUrl`
  and `FileUrlSignature` on the four create-options records are nullable and NOT `required` —
  the Stream overloads fill them from the upload step (via `options with { … }`) and validate at
  call time (direct overload: must be set; Stream overload: must be unset).
- **Enum policy**: C# enums only for values the SDK *writes* (delivery product, print mode, …);
  everything read-only (statuses, event codes, source, abilities) stays `string` — Pingen documents
  these sets as open.
- **Errors**: one exception type, `PingenException`, carrying status code, parsed `PingenError` list,
  `RequestId` (X-Request-Id) and `RetryAfter` when present. Absence is modeled by the API itself
  (404 throws — an HTTP client is not a repository; no null-for-absence here).

---

## §4 API wire conventions

Every sub-agent building request/response code must honor all of these. They come from the spec's
prose sections and hold across all resources.

**Hosts.** Production API `https://api.pingen.com`, staging API `https://api-staging.pingen.com`.
Identity (OAuth) is a separate host: `https://identity.pingen.com` / `https://identity-staging.pingen.com`.
The spec declares no `servers` — hosts are SDK configuration (`PingenEnvironment` + overrides).

**Auth.** OAuth2 `client_credentials`: `POST {identity}/auth/access-tokens`,
`Content-Type: application/x-www-form-urlencoded`, fields `grant_type=client_credentials`,
`client_id`, `client_secret`, optional `scope` (space-separated: `letter ebill email batch webhook
organisation_read user`). Response: `{"token_type":"Bearer","expires_in":43200,"access_token":"…"}`
— 12 hours, no refresh token. Cache the token; re-request when expired (refresh 60 s early); 401
means wrong/expired token → invalidate, refetch, retry the request once. Every API call sends
`Authorization: Bearer {token}`. Note: scope `user` is required by `/user*` endpoints although the
securityScheme scope list omits it — request it when configuring scopes explicitly.

**Content type.** All JSON bodies (requests, responses, errors) are `application/vnd.api+json`
(JSON:API). Send it as Content-Type on every POST/PATCH/DELETE-with-body; also send `Accept:
application/vnd.api+json`. The token endpoint is the only form-urlencoded call.

**JSON:API envelopes.**
- Single: `{ "data": { "id", "type", "attributes", "relationships", "links": {"self"}, "meta"? }, "included"?: [...] }`.
- List: `{ "data": [items], "included"?: [...], "links": {first,last,prev,next,self}, "meta": {current_page,last_page,per_page,from,to,total} }`.
- `meta.abilities.self` (kebab-case ability → `"ok"|"state"|"permission"`) appears on single-resource
  responses only, never on list items → model `Meta` as nullable and abilities as
  `IReadOnlyDictionary<string, string>`.
- To-one relationships: `{ "links"?: {"related"}, "data": {"id","type"} }`. To-many relationships
  (`events`, `associations`, `notifications`) are **link-only**: `{ "links": { "related": { "href",
  "meta": {"count"} } } }` — no `data` array despite the spec claiming one. Model accordingly.
- POST bodies: `{ "data": { "type": "...", "attributes": {...}, "relationships"?: {...} } }` — no `id`.
  PATCH send/edit/delete bodies **require** `data.id` (duplicating the path id) — build it from the
  method's id parameter, never ask the caller twice.

**Collections.** Pagination `page[number]` (default 1) and `page[limit]` (default 20, max 100).
Sort: `sort=field,-other` (`-` = DESC); defaults are `created_at` for resource lists, `real_id` for
event lists; the four org-wide letter-event lists AND `webhooks.index` accept **no** sort at all
(a supplied `Sort` on those calls is simply not emitted — don't error). Filter: the `filter`
query param carries a JSON string — `{"attr":"value"}`, combinators `{"and":[…]}` / `{"or":[…]}`,
comparison operators are *prepended to the value string*: `<`, `<=`, `>`, `>=`, `!`, `~`.
Full-text: `q`. Sparse fieldsets: `fields[letters]=…`, `fields[organisations]=…`, keyed by JSON:API
type. Compound documents: `include=` (to-one only). Event lists take `language` (default `en-GB`)
which localizes the event `name`.

**Timestamps.** Wire format is `Y-m-d\TH:i:sO` → `2021-11-19T09:42:48+0100` — **no colon in the
offset**, so STJ's default DateTimeOffset converter rejects it. One custom `JsonConverter<DateTimeOffset>`
(and a nullable twin) using `DateTimeOffset.Parse` with `CultureInfo.InvariantCulture` handles both
`+0100` and `+01:00`; tolerate empty string/null as null for the nullable twin (e.g. `submitted_at`
before submission — model every `submitted_at`-like field as `DateTimeOffset?`). Invoice dates are
plain `Y-m-d` → `DateOnly`.

**Errors.** All 4XX/5XX bodies: `{ "errors": [ { "code", "title", "detail", "source": { "pointer",
"parameter" } } ] }` — model every member as nullable string; `code` values are undocumented opaque
strings. Notable statuses: 401 (token), 409 (duplicate Idempotency-Key still in flight), 422
(validation), 429 (rate limit — headers `Retry-After` seconds + `X-Rate-Limit-Reset`), 503
(maintenance). Every response carries `X-Request-Id` — surface it on `PingenException`.

**Rate limits.** 300 requests/minute per user; every response has `X-Ratelimit-Limit` /
`X-Ratelimit-Remaining`. The SDK does not auto-retry (v1); it surfaces `RetryAfter` on the exception.

**Idempotency.** Optional `Idempotency-Key` header (1–64 chars, UUIDv4 suggested) on POST/PATCH;
keys live 24 h; replays return the original response with header `Idempotent-Replayed: true`.
Exposed via `PingenRequestOptions.IdempotencyKey` — never auto-generated (v1).

**File upload (the 3-step flow).** Used by letters/emails/ebills/batches create:
1. `GET /file-upload` → `data.attributes: { url, url_signature, expires_at }` (presigned, single-use).
2. `PUT` the **raw** bytes to `url` — not multipart, **no Authorization header** (use the
   `"PingenFiles"` client). PDF for letters/emails/ebills; ZIP or merged PDF for batches.
3. POST the create endpoint with `file_url` + `file_url_signature` copied verbatim.

**File download (302 endpoints).** `letters|emails|ebills/{id}/file` and `events/{eventId}/image`
respond `302 Found` with the presigned URL in the `Location` header and no body. The API client has
`AllowAutoRedirect = false`: read `Location` (the spec mistypes its schema as `number`; it is a URL),
then fetch it with the `"PingenFiles"` client (no Bearer — auth headers break presigned URLs).

**Webhooks (inbound).** Pingen POSTs JSON:API payloads to the subscribed URL with header
`Signature` = lowercase hex HMAC-SHA256 of the **raw body bytes**, keyed with the webhook's
`signing_key`. Respond 2xx to ack; otherwise Pingen retries at 1 m, 5 m, 10 m, 1 h, 2 h, 4 h, then
gives up (at-least-once delivery — surface `data.id` for dedup). Verify with
`CryptographicOperations.FixedTimeEquals` against the raw bytes; never re-serialize.

**Staging simulation.** On staging nothing is delivered; outcomes are driven by filename suffixes
(`*_simulate_undeliverable.pdf`, `*_simulate_unprintable.pdf`, `*_simulate_cancellable.pdf`; ebills:
`*_simulate_refused|approved|paid|cancellable.pdf`). Mention in README; no SDK code needed.

**Spec quirks sub-agents must not "fix":** `relationships` blocks in create schemas carry a bogus
`required: [""]` (treat relationships/preset as optional); `RelatedManyOutput` declares `data`
required but defines none (model links-only); the 302 `Location` header schema says `number` (it's a
string); event-image path placeholders are `{letterEventId}` / `{deliverableEventId}` regardless of
the component parameter names; the email create/show operations declare scope `letter` (not `email`)
— document, don't work around; `deliveries.ebills.send` takes **no body** while `letters.send`
requires one; `batches.delete` is a **DELETE with a required JSON body**.

---

## §5 Project layout

One project, vertical feature folders, one concept per file (satellite types co-located). Namespace
= folder. Files below are the complete production inventory; tests mirror this tree (§10).

```
src/Pingen.Client/Pingen.Client/
├── PingenClient.cs                  # typed-client hub: lazy service props + internal request core
├── PingenConfiguration.cs           # AddPingen(this IServiceCollection) [+ Action<PingenOptions> overload]
├── Options/
│   ├── PingenOptions.cs             # ClientId, ClientSecret, Environment, Scopes, BaseAddress?, IdentityAddress?
│   ├── PingenOptionsValidator.cs    # IValidateOptions<PingenOptions> (hand-rolled, no FluentValidation)
│   └── PingenEnvironment.cs         # enum Production | Staging (+ extension resolving default hosts)
├── Authentication/
│   ├── PingenAccessTokens.cs        # singleton token cache; client_credentials POST; SemaphoreSlim
│   ├── PingenAuthenticationHandler.cs # DelegatingHandler: Bearer + single 401 retry
│   └── AccessToken.cs               # record: token, expiry (satellite of PingenAccessTokens is fine too)
├── Common/
│   ├── Json/
│   │   ├── PingenJson.cs            # cached JsonSerializerOptions (converters, WhenWritingNull)
│   │   ├── PingenDateTimeConverter.cs   # +0100-offset tolerant (nullable + non-null handling)
│   │   └── PingenEnumConverter.cs   # enum wire names via per-member [JsonStringEnumMemberName] — NOT a
│   │                                #   naming policy: snake_case is merely common; BatchIcon is kebab-case
│   │                                #   (wave-hand, percent-tag) and batch send products are electronic_email/_ebill
│   ├── JsonApi/
│   │   ├── SingleDocument.cs        # SingleDocument<TResource>: Data, Included (JsonElement[])
│   │   ├── ListDocument.cs          # ListDocument<TResource>: Data, Included, Links, Meta
│   │   ├── ListLinks.cs             # first/last/prev/next/self (nullable strings)
│   │   ├── ListMeta.cs              # current_page/last_page/per_page/from/to/total (int)
│   │   ├── ResourceLinks.cs         # self
│   │   ├── ResourceMeta.cs          # Abilities: IReadOnlyDictionary<string,string> under "abilities"."self"
│   │   ├── Relationship.cs          # to-one: Data {Id, Type} + related link; RelatedCollection: href+count
│   │   └── RequestDocument.cs       # write-side: Data{Type, Id?, Attributes, Relationships?}; PresetRelationship
│   ├── PingenList.cs                # PingenList<T> : IReadOnlyList<T> — Data, Links, Meta
│   ├── PingenListOptions.cs         # PageNumber, PageLimit, Sort, Filter, Search, Include, Language, Fields
│   ├── PingenFilter.cs              # filter-JSON builder: Where/Not/GreaterThan…/And/Or → ToJson()
│   ├── PingenRequestOptions.cs      # per-call: IdempotencyKey
│   ├── PingenError.cs               # code/title/detail/source(pointer,parameter)
│   └── PingenException.cs           # StatusCode, Errors, RequestId, RetryAfter
├── Files/
│   ├── FileService.cs               # RequestUploadAsync, UploadAsync(Stream), DownloadAsync(Uri)
│   └── FileUpload.cs                # resource: Url, UrlSignature, ExpiresAt
├── Deliveries/
│   ├── DeliverableEvent.cs          # shared event resource (letters_events / deliverables_events)
│   ├── ValueTypes/
│   │   ├── DeliveryProduct.cs       # Fast, Cheap, Bulk, Premium, Registered
│   │   ├── PrintMode.cs             # Simplex, Duplex
│   │   ├── PrintSpectrum.cs         # Color, Grayscale
│   │   └── AddressPosition.cs       # Left, Right
│   ├── Letters/
│   │   ├── LetterService.cs
│   │   ├── Letter.cs                # Letter + LetterAttributes + LetterRelationships (+ LetterFont)
│   │   ├── LetterCreateOptions.cs
│   │   ├── LetterSendOptions.cs
│   │   ├── LetterMetaData.cs        # recipient/sender address blocks (create + send)
│   │   ├── LetterPriceOptions.cs    # price-calculator input (+ PaperType enum lives here)
│   │   └── LetterPrice.cs           # price-calculator result (currency, price)
│   ├── Emails/
│   │   ├── EmailService.cs
│   │   ├── Email.cs                 # Email + EmailAttributes + EmailRelationships
│   │   ├── EmailCreateOptions.cs
│   │   └── EmailMetaData.cs
│   └── Ebills/
│       ├── EbillService.cs
│       ├── Ebill.cs                 # Ebill + EbillAttributes + EbillRelationships
│       ├── EbillCreateOptions.cs
│       └── EbillMetaData.cs
├── Batches/
│   ├── BatchService.cs
│   ├── Batch.cs                     # Batch + BatchAttributes + BatchRelationships
│   ├── BatchEvent.cs
│   ├── BatchStatistics.cs           # + BatchLetterGroup, BatchLetterCountry
│   ├── BatchCreateOptions.cs        # + grouping enums colocated: BatchGroupingType, BatchSplitType, BatchSplitPosition
│   ├── BatchEditOptions.cs
│   ├── BatchDeleteOptions.cs        # WithLetters (required), WithDeliverables
│   ├── BatchSendOptions.cs          # static factories Post(...)/Email()/Ebill() — oneOf by data.type
│   └── ValueTypes/
│       └── BatchIcon.cs             # 16-value enum (input-only)
├── Organisations/
│   ├── OrganisationService.cs
│   └── Organisation.cs              # Organisation + OrganisationAttributes + relationships
├── Users/
│   ├── UserService.cs
│   ├── User.cs                      # User + UserAttributes
│   └── Association.cs               # Association + AssociationAttributes (role/status strings)
└── Webhooks/
    ├── WebhookService.cs            # management CRUD
    ├── Webhook.cs                   # Webhook + WebhookAttributes
    ├── WebhookCreateOptions.cs      # EventCategory, Url, SigningKey
    ├── WebhookEventCategory.cs      # Issues, Sent, Undeliverable, Delivered, ChannelSubscriptions
    ├── PingenWebhook.cs             # static: ConstructEvent / ParseEvent / VerifySignature
    └── Payloads/
        ├── WebhookEvent.cs          # abstract base: Id, Type, Url, CreatedAt, Organisation rel
        ├── WebhookIssueEvent.cs     # + Reason, Deliverable + Event rels
        ├── WebhookSentEvent.cs
        ├── WebhookDeliveredEvent.cs
        ├── WebhookUndeliverableEvent.cs  # + Reason, CorrectedAddress (nullable defensive)
        └── WebhookChannelSubscriptionEvent.cs # identifier/email/name/address/status/approved_at + channel_ebill rel
```

---

## §6 Public API surface

### 6.1 Registration (DI-native entry point)

```csharp
namespace Pingen.Client;

public static class PingenConfiguration
{
    /// <summary>Registers the Pingen client, binding options from the "Pingen" configuration section.</summary>
    public static IServiceCollection AddPingen(this IServiceCollection services) { ... }

    /// <summary>Registers the Pingen client with options configured in code.</summary>
    public static IServiceCollection AddPingen(this IServiceCollection services, Action<PingenOptions> configure) { ... }
}
```

Both overloads: `AddOptions<PingenOptions>()` (+ `BindConfiguration("Pingen")` in the parameterless
one) `.ValidateOnStart()`; `services.AddSingleton<IValidateOptions<PingenOptions>, PingenOptionsValidator>()`;
`AddSingleton<PingenAccessTokens>()`; `AddTransient<PingenAuthenticationHandler>()`;
named client `"PingenIdentity"` (BaseAddress = identity host from options);
named client `"PingenFiles"` (untouched defaults, no auth);
`AddHttpClient<PingenClient>(...)` with BaseAddress = API host,
`ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })`,
`.AddHttpMessageHandler<PingenAuthenticationHandler>()`;
`AddTransient<…Service>()` for all eight services. Return `services`.

**Ownership note:** this paragraph describes the FINAL state. Phase 2 builds `PingenConfiguration`
with everything EXCEPT the eight `AddTransient<…Service>()` lines and builds `PingenClient` with
the request core but NO hub properties; phase 8 (sole owner of both files thereafter) adds all
service registrations and all hub properties in one pass. Phases 3–7 never touch these two files.

`PingenOptions` (class, `{ get; init; }`, XML docs on every property — house options style):
`ClientId` (string?), `ClientSecret` (string?), `Environment` (`PingenEnvironment`, default
`Production`), `Scopes` (string?, space-separated, null = all registered), `BaseAddress` (Uri?,
overrides environment default), `IdentityAddress` (Uri?). Validator: ClientId/ClientSecret non-empty.

### 6.2 The client hub

```csharp
public class PingenClient(HttpClient httpClient)
{
    public LetterService Letters => ...;          // lazy ??= per service (Stripe hub style)
    public EmailService Emails => ...;
    public EbillService Ebills => ...;
    public BatchService Batches => ...;
    public OrganisationService Organisations => ...;
    public UserService Users => ...;
    public WebhookService Webhooks => ...;
    public FileService Files => ...;              // needs IHttpClientFactory — see note

    // internal request core used by services:
    // Task<T> GetAsync<T>(string path, CancellationToken)
    // Task<T> SendAsync<T>(HttpMethod, string path, object? body, PingenRequestOptions?, CancellationToken)
    // Task SendAsync(...)                        (202/204, no payload)
    // Task<Uri> GetLocationAsync(string path, CancellationToken)   (302 endpoints)
}
```

Note: `FileService` also needs the `"PingenFiles"` client → `PingenClient`'s constructor takes
`(HttpClient httpClient, IHttpClientFactory httpClientFactory)`; the typed-client registration
provides both. Services are `public class XService(PingenClient client)` — primary constructors,
no fields, no interfaces. All methods are async-only, `Async`-suffixed, and take
`CancellationToken cancellationToken = default` as the last parameter.

Query building: one internal helper composes `page[number]`, `page[limit]`, `sort`, `filter`, `q`,
`include`, `language`, `fields[{type}]` from `PingenListOptions` (all nullable, only set values
emitted; `Fields` is `IReadOnlyDictionary<string,string>?` keyed by JSON:API type). Escape values
with `Uri.EscapeDataString`.

**PingenFilter** (complete public surface — phase 1 builds exactly this, §11's README shows it):

```csharp
public record PingenFilter                       // opaque node; ToJson() renders the §4 grammar
{
    public static PingenFilter Where(string attribute, string value);          // {"attr":"value"}
    public static PingenFilter Not(string attribute, string value);            // "!value"
    public static PingenFilter Contains(string attribute, string value);       // "~value"
    public static PingenFilter GreaterThan(string attribute, string value);    // ">value"
    public static PingenFilter GreaterOrEqual(string attribute, string value); // ">=value"
    public static PingenFilter LessThan(string attribute, string value);       // "<value"
    public static PingenFilter LessOrEqual(string attribute, string value);    // "<=value"
    // date convenience overloads on the four comparison factories + Where:
    //   DateOnly → "yyyy-MM-dd"; DateTimeOffset → "yyyy-MM-ddTHH:mm:sszzz"
    public static PingenFilter And(params PingenFilter[] filters);             // {"and":[…]}
    public static PingenFilter Or(params PingenFilter[] filters);              // {"or":[…]}
    public static PingenFilter Raw(string json);                               // escape hatch, emitted verbatim
    public string ToJson();
}
```

`PingenListOptions.Filter` is typed `PingenFilter?`; the query builder calls `ToJson()`.

### 6.3 Service surfaces (definition of done per service)

`PingenListOptions? options = null` and trailing `CancellationToken` omitted below for brevity;
mutating methods additionally take `PingenRequestOptions? requestOptions = null`.

**LetterService**
```
Task<PingenList<Letter>>          ListAsync(Guid organisationId, PingenListOptions?)
IAsyncEnumerable<Letter>          ListAutoPagingAsync(Guid organisationId, PingenListOptions?)
Task<Letter>                      CreateAsync(Guid organisationId, LetterCreateOptions, PingenRequestOptions?)
Task<Letter>                      CreateAsync(Guid organisationId, Stream content, LetterCreateOptions, PingenRequestOptions?)   // 3-step upload; options.FileUrl/Signature must be unset
Task<Letter>                      GetAsync(Guid organisationId, Guid letterId)
Task                              DeleteAsync(Guid organisationId, Guid letterId)                       // 204
Task                              CancelAsync(Guid organisationId, Guid letterId, PingenRequestOptions?) // PATCH, 202, no body
Task<Letter>                      SendAsync(Guid organisationId, Guid letterId, LetterSendOptions, PingenRequestOptions?) // 200
Task<Uri>                         GetFileLocationAsync(Guid organisationId, Guid letterId)              // 302 → Location
Task<Stream>                      DownloadFileAsync(Guid organisationId, Guid letterId)                 // location + unauthenticated GET
Task<PingenList<DeliverableEvent>> ListEventsAsync(Guid organisationId, Guid letterId, PingenListOptions?)
Task<Uri>                         GetEventImageLocationAsync(Guid organisationId, Guid letterId, Guid eventId)
Task<Stream>                      DownloadEventImageAsync(Guid organisationId, Guid letterId, Guid eventId)
Task<PingenList<DeliverableEvent>> ListSentEventsAsync(Guid organisationId, PingenListOptions?)         // no sort support
Task<PingenList<DeliverableEvent>> ListDeliveredEventsAsync(Guid organisationId, PingenListOptions?)
Task<PingenList<DeliverableEvent>> ListIssueEventsAsync(Guid organisationId, PingenListOptions?)
Task<PingenList<DeliverableEvent>> ListUndeliverableEventsAsync(Guid organisationId, PingenListOptions?)
Task<LetterPrice?>                CalculatePriceAsync(Guid organisationId, LetterPriceOptions, PingenRequestOptions?)  // null when the API answers 202 with no body
```

**EmailService** — `ListAsync`, `ListAutoPagingAsync`, `CreateAsync` (+ Stream overload), `GetAsync`,
`DeleteAsync`, `CancelAsync`, `GetFileLocationAsync`, `DownloadFileAsync`, `ListEventsAsync`,
`GetEventImageLocationAsync`, `DownloadEventImageAsync`. (No send — emails only auto-send.)

**EbillService** — like EmailService **plus** `SendAsync(Guid organisationId, Guid ebillId,
PingenRequestOptions?)` → `Task<Ebill>` (PATCH with **no body**, returns 200 envelope).

**BatchService**
```
ListAsync / ListAutoPagingAsync                              → PingenList<Batch>
CreateAsync(orgId, BatchCreateOptions)                       → Batch (201)
CreateAsync(orgId, Stream content, BatchCreateOptions)      → Batch (Stream-before-options, mirroring Letters)
GetAsync(orgId, batchId)                                     → Batch
EditAsync(orgId, batchId, BatchEditOptions)                  → Task (PATCH, 202 no body; body carries data.id)
DeleteAsync(orgId, batchId, BatchDeleteOptions)              → Task (DELETE with required JSON body!)
CancelAsync(orgId, batchId)                                  → Task (PATCH, 202, no body)
SendAsync(orgId, batchId, BatchSendOptions)                  → Batch (200)
ListEventsAsync(orgId, batchId, PingenListOptions?)          → PingenList<BatchEvent>
GetStatisticsAsync(orgId, batchId)                           → BatchStatistics
```
`BatchSendOptions` has a private ctor and static factories mapping the oneOf:
`BatchSendOptions.Post(DeliveryProduct, PrintMode, PrintSpectrum)` → type `batches_channel_post_send`;
`.Email()` → `batches_channel_email_send` + `delivery_product: "electronic_email"`;
`.Ebill()` → `batches_channel_ebill_send` + `"electronic_ebill"`.

**OrganisationService** — `ListAsync(PingenListOptions?)` / `ListAutoPagingAsync` /
`GetAsync(Guid organisationId)`.

**UserService** — `GetAsync()` (singleton `/user`), `ListAssociationsAsync(PingenListOptions?)` /
`ListAssociationsAutoPagingAsync` (`/user/associations`).

**WebhookService** — `ListAsync(orgId, PingenListOptions?)`, `CreateAsync(orgId,
WebhookCreateOptions)`, `GetAsync(orgId, webhookId)`, `DeleteAsync(orgId, webhookId)`.

**FileService**
```
Task<FileUpload> RequestUploadAsync()                        // GET /file-upload
Task             UploadAsync(FileUpload target, Stream content)  // raw PUT, PingenFiles client, no auth
Task<FileUpload> UploadAsync(Stream content)                 // convenience: request + PUT, returns the FileUpload
Task<Stream>     DownloadAsync(Uri location)                 // unauthenticated GET (302 targets)
```

**PingenWebhook** (static)
```
static WebhookEvent ConstructEvent(string payload, string signatureHeader, string signingKey)  // verify + parse; PingenException on bad signature
static WebhookEvent ParseEvent(string payload)                                                 // no verification
static bool         VerifySignature(string payload, string signatureHeader, string signingKey) // FixedTimeEquals over raw UTF-8 bytes
```
`ConstructEvent` dispatches on `data.type` (`webhook_issues|webhook_sent|webhook_delivered|
webhook_undeliverable|webhook_channel_subscriptions`) via a `JsonDocument` peek, returning the
concrete derived record; unknown type → `PingenException`.

**Auto-paging semantics.** `ListAutoPagingAsync` starts from `options` (or page 1), yields items,
and refetches with `page[number] + 1` while `Meta.CurrentPage < Meta.LastPage`; empty pages stop.
`[EnumeratorCancellation]` on the token.

---

## §7 Endpoint coverage matrix

Feature complete = every row implemented and tested. 49 API operations + 5 inbound payload models.
(The spec's ~180 pathless billing/template stubs and the `template` scope are documented no-ops —
out of scope.)

| # | Method & path | operationId | SDK method |
|---|---|---|---|
| 1 | GET `/file-upload` | files.file-upload | `Files.RequestUploadAsync` |
| 2 | GET `/organisations` | organisations.index | `Organisations.ListAsync` |
| 3 | GET `/organisations/{org}` | organisations.show | `Organisations.GetAsync` |
| 4 | GET `/user` | users.show | `Users.GetAsync` |
| 5 | GET `/user/associations` | user.associations.list | `Users.ListAssociationsAsync` |
| 6 | GET `…/deliveries/letters` | letters.list | `Letters.ListAsync` |
| 7 | POST `…/deliveries/letters` | letters.create | `Letters.CreateAsync` |
| 8 | GET `…/letters/{id}` | letters.show | `Letters.GetAsync` |
| 9 | DELETE `…/letters/{id}` | letters.delete | `Letters.DeleteAsync` |
| 10 | PATCH `…/letters/{id}/cancel` | letters.cancel | `Letters.CancelAsync` |
| 11 | PATCH `…/letters/{id}/send` | letters.send | `Letters.SendAsync` |
| 12 | GET `…/letters/{id}/file` | letters.file | `Letters.GetFileLocationAsync` / `DownloadFileAsync` |
| 13 | GET `…/letters/{id}/events` | letters.events | `Letters.ListEventsAsync` |
| 14 | GET `…/letters/{id}/events/{eventId}/image` | letters.events.image | `Letters.GetEventImageLocationAsync` / `DownloadEventImageAsync` |
| 15 | GET `…/letters/events/sent` | organisations.letters.events.sent | `Letters.ListSentEventsAsync` |
| 16 | GET `…/letters/events/delivered` | organisations.letters.events.delivered | `Letters.ListDeliveredEventsAsync` |
| 17 | GET `…/letters/events/issues` | organisations.letters.events.issues | `Letters.ListIssueEventsAsync` |
| 18 | GET `…/letters/events/undeliverable` | organisations.letters.events.undeliverable | `Letters.ListUndeliverableEventsAsync` |
| 19 | POST `…/letters/price-calculator` | letters.price-calculator | `Letters.CalculatePriceAsync` |
| 20 | GET `…/deliveries/emails` | deliveries.emails.list | `Emails.ListAsync` |
| 21 | POST `…/deliveries/emails` | deliveries.emails.create | `Emails.CreateAsync` |
| 22 | GET `…/emails/{id}` | deliveries.emails.show | `Emails.GetAsync` |
| 23 | DELETE `…/emails/{id}` | deliveries.emails.delete | `Emails.DeleteAsync` |
| 24 | PATCH `…/emails/{id}/cancel` | deliveries.emails.cancel | `Emails.CancelAsync` |
| 25 | GET `…/emails/{id}/file` | deliveries.emails.file | `Emails.GetFileLocationAsync` / `DownloadFileAsync` |
| 26 | GET `…/emails/{id}/events` | deliveries.emails.events | `Emails.ListEventsAsync` |
| 27 | GET `…/emails/{id}/events/{eventId}/image` | deliveries.emails.events.image | `Emails.GetEventImageLocationAsync` / `DownloadEventImageAsync` |
| 28 | GET `…/deliveries/ebills` | deliveries.ebills.list | `Ebills.ListAsync` |
| 29 | POST `…/deliveries/ebills` | deliveries.ebills.create | `Ebills.CreateAsync` |
| 30 | GET `…/ebills/{id}` | deliveries.ebills.show | `Ebills.GetAsync` |
| 31 | DELETE `…/ebills/{id}` | deliveries.ebills.delete | `Ebills.DeleteAsync` |
| 32 | PATCH `…/ebills/{id}/cancel` | deliveries.ebills.cancel | `Ebills.CancelAsync` |
| 33 | PATCH `…/ebills/{id}/send` | deliveries.ebills.send | `Ebills.SendAsync` (no body) |
| 34 | GET `…/ebills/{id}/file` | deliveries.ebills.file | `Ebills.GetFileLocationAsync` / `DownloadFileAsync` |
| 35 | GET `…/ebills/{id}/events` | deliveries.ebills.events | `Ebills.ListEventsAsync` |
| 36 | GET `…/ebills/{id}/events/{eventId}/image` | deliveries.ebills.events.image | `Ebills.GetEventImageLocationAsync` / `DownloadEventImageAsync` |
| 37 | GET `…/batches` | batches.list | `Batches.ListAsync` |
| 38 | POST `…/batches` | batches.create | `Batches.CreateAsync` |
| 39 | GET `…/batches/{id}` | batches.show | `Batches.GetAsync` |
| 40 | PATCH `…/batches/{id}` | batches.edit | `Batches.EditAsync` |
| 41 | DELETE `…/batches/{id}` | batches.delete | `Batches.DeleteAsync` (required body) |
| 42 | PATCH `…/batches/{id}/cancel` | batches.cancel | `Batches.CancelAsync` |
| 43 | PATCH `…/batches/{id}/send` | batches.send | `Batches.SendAsync` |
| 44 | GET `…/batches/{id}/events` | batches.events | `Batches.ListEventsAsync` |
| 45 | GET `…/batches/{id}/statistics` | organisations.batches.statistics.details | `Batches.GetStatisticsAsync` |
| 46 | GET `…/webhooks` | webhooks.index | `Webhooks.ListAsync` |
| 47 | POST `…/webhooks` | webhooks.create | `Webhooks.CreateAsync` |
| 48 | GET `…/webhooks/{id}` | webhooks.show | `Webhooks.GetAsync` |
| 49 | DELETE `…/webhooks/{id}` | webhooks.destroy | `Webhooks.DeleteAsync` |

Inbound payload models (documentation-only spec paths → typed records + `PingenWebhook` parsing):
`webhook_issues`, `webhook_sent`, `webhook_delivered`, `webhook_undeliverable`,
`webhook_channel_subscriptions`.

---

## §8 Resource model reference

Wire names are exact; C# names are PascalCase with `[JsonPropertyName]` on **every** property
(house rule: independent of any global naming policy). "string (open)" = do NOT model as enum.
All timestamps `DateTimeOffset` (`DateTimeOffset?` where noted) via the custom converter.

**Letter attributes** (`type: "letters"`): `status` string — the spec deliberately declares NO
enum ("we do not provide a complete list of statuses or event codes"); values observed on the
platform include validating, valid, invalid, action_required, fixing, submitted, awaiting_credits,
accepted, inspection, processing, printing, transferring, sent, delivered, undeliverable,
unprintable, rejected, expired, cancelling, cancelled, cancelled_expired — cite that list in the
XML doc as "observed values", keep string —, `file_original_name`,
`file_pages` int, `address` string (multiline), `address_position` string (left|right),
`country`, `delivery_product` string (spec's read enum is fast|cheap|bulk|premium|registered, keep
string anyway — electronic products appear via batch channels), `print_mode`, `print_spectrum`,
`price_currency`, `price_value` decimal, `paper_types` string[], `fonts` array of `{name,
is_embedded bool}` (tolerate 0/1 as bool — spec example is numeric; a lenient bool converter is
acceptable), `source` string (open: app, api, batch, integration_*), `tracking_number` string?,
`submitted_at` DateTimeOffset?, `created_at`, `updated_at`. Relationships: `organisation` (to-one),
`batch` (to-one), `events` (link-only count). Meta (single GET only): abilities dictionary.

**LetterCreateOptions** → attributes: `file_original_name` (required, ≤255), `file_url` (≤1000)
and `file_url_signature` (≤60) — wire-required but nullable non-`required` in C# per the §3
exemption: the Stream overload fills them from the upload and validates at call time —,
`auto_send` (required bool), `address_position?`,
`delivery_product?`, `print_mode?`, `print_spectrum?`, `meta_data?` (LetterMetaData), plus optional
`PresetId` (Guid?) → `relationships.preset.data {id, type:"presets"}`.
**LetterSendOptions** → `delivery_product`, `print_mode`, `print_spectrum` all required, `meta_data?`.
**LetterMetaData**: `recipient` + `sender`, each `{name ≤45, street ≤40, pobox ≤45, number ≤10,
zip ≤8, city ≤25, country (2 chars)}`. The spec formally marks all seven sub-fields required, but
its field descriptions say either `pobox` OR `street` is provided — deliberate deviation: model all
seven as optional strings (nulls omitted on the wire) so both address shapes are expressible.
**LetterPriceOptions** (`type: "letter_price_calculator"`): `country` (required), `paper_types`
(required array of enum PaperType: normal, qr, sepa_at, sepa_de — one per page), `print_mode`,
`print_spectrum`, `delivery_product` (all required). Result `LetterPrice`: `currency`, `price`
decimal. The spec declares both 200-with-body and 202-with-empty-body: the method returns
`Task<LetterPrice?>` and maps an empty 202 to null (XML-documented).

**Email attributes** (`type: "emails"`): `status` string (open), `file_original_name`, `file_pages`
int, `recipient_identifier`, `price_currency`, `price_value` decimal, `source` string,
`submitted_at` DateTimeOffset?, `created_at`, `updated_at`. Relationships organisation/batch/events
as letters. **EmailCreateOptions**: `file_original_name`, `file_url`, `file_url_signature`,
`auto_send` (required) + `meta_data?` `{sender_name, recipient_email, recipient_name, reply_email,
reply_name, subject (≤255 each), content ≤16384}` — ALL seven required whenever `meta_data` is
present, so `EmailMetaData` uses `required` members — + `PresetId?`.

**Ebill attributes** (`type: "ebills"`): `status` string (open), `file_original_name`, `file_pages`
int, `recipient_identifier`, `recipient_address` (multiline), `invoice_number`, `invoice_date`
DateOnly, `invoice_due_date` DateOnly, `invoice_value` decimal, `invoice_currency`, `invoice_iban`,
`invoice_address`, `invoice_reference`, `price_currency`, `price_value` decimal, `source`,
`submitted_at` DateTimeOffset?, `created_at`, `updated_at`. **EbillCreateOptions**: the four
required file/auto_send fields + `meta_data?` `{invoice_number ≤100, invoice_date (Y-m-d),
invoice_due_date, recipient_identifier}` (all four required when present) + `PresetId?`.

**DeliverableEvent** (letters: `type "letters_events"`; emails/ebills: `"deliverables_events"` —
same attribute shape, one shared record; keep `Type` string): `code` string (open), `name` string
(localized), `producer`, `location`, `has_image` bool, `data` string[], `emitted_at`, `created_at`,
`updated_at`. Parent relationship: the JSON key differs per channel — `"letter"` on letters events,
`"email"` on email events, `"ebill"` on ebill events. Model `DeliverableEventRelationships` with
three nullable to-one properties (`[JsonPropertyName("letter")] Letter`, `("email")` `Email`,
`("ebill")` `Ebill`) plus a computed `Parent` returning whichever is set — one record serves all
three channels.

**Batch attributes** (`type: "batches"`): `name`, `channel_type` string (post|ebill|email),
`icon` string (open on read), `status` string (open), `file_original_name`, `letter_count` int,
`deliverable_count` int, `address_position`, `print_mode`, `print_spectrum`, `price_currency`,
`price_value` decimal, `source`, `submitted_at` DateTimeOffset?, `created_at`, `updated_at`.
**BatchCreateOptions**: `name` (required, 5–100), `icon` (required, BatchIcon enum: campaign,
megaphone, wave-hand, flash, rocket, bell, percent-tag, percent-badge, present, receipt, document,
information, calendar, newspaper, crown, virus — mind kebab wire names), `file_original_name`
(required 5–255), `file_url`/`file_url_signature` (required), `grouping_type` (required: zip|merge),
`grouping_options_split_type` (required: file|page|custom|qr_invoice), optional `channel_type`
(post|ebill|email), `address_position?`, `grouping_options_split_size?` int 1–10,
`grouping_options_split_separator?` ≤20, `grouping_options_split_position?` (first_page|last_page),
`PresetId?`. **BatchEditOptions**: `name?`, `icon?`. **BatchDeleteOptions**: `with_letters`
(required bool), `with_deliverables?` bool. **BatchEvent** (`type "batches_events"`): own record —
like DeliverableEvent but **without** `has_image` (spec lists code, name, producer, location, data,
emitted_at, created_at, updated_at) and with a `batch` relationship; batches have no event-image
endpoint. **BatchStatistics** (`type
"batch_details_statistics"`): `letter_validating` int, `letter_groups` `[{name, count}]`,
`letter_countries` `[{country, count}]`, `letter_regions` same shape.

**Organisation attributes** (`type: "organisations"`): `name`, `status` string (open: active,
termination_confirmed, pending_deletion), `plan`, `billing_mode` string (prepaid|postpaid),
`billing_currency`, `billing_balance` decimal, `missing_credits` decimal, `edition`,
`default_country`, `default_address_position`, `data_retention_addresses` int,
`data_retention_pdf` int, `limits_monthly_letters_count` int, `limits_monthly_ebills_count` int,
`limits_monthly_emails_count` int, `color`, `flags` string[], `created_at`, `updated_at`.

**User attributes** (`type: "users"`, singleton `/user`): `email`, `first_name`, `last_name`,
`status` string (open), `language` string (en-GB, de-DE, …), `edition`, `flags` string[],
`created_at`, `updated_at`. **Association attributes** (`type: "associations"`): `role` string
(owner|manager), `status` string (pending|active|blocked), `created_at`, `updated_at`;
relationship `organisation`.

**FileUpload attributes** (`type: "file_uploads"`): `url` (presigned), `url_signature`,
`expires_at` DateTimeOffset. No relationships/meta.

**Webhook attributes** (`type: "webhooks"`): `event_category` (enum on write:
issues|sent|undeliverable|delivered|channel_subscriptions; string on read is fine to keep as the
enum — closed set — or string; pick enum since SDK writes it), `url` ≤200, `signing_key` (20–32
chars, echoed back in cleartext). Relationship `organisation`. No update endpoint — delete + create.

**Webhook payloads** (inbound): common `data.{id, type, attributes.{url, created_at}}` +
`relationships.organisation`; issues adds `reason` + `deliverable`/`event` rels; undeliverable adds
`reason` + `corrected_address {name, street, number, zip, city}` (model nullable) + rels; sent /
delivered are the base shape + rels; channel_subscriptions has `identifier`, `email`, `name`,
`address`, `status` (active|inactive|requested — keep string), `approved_at`, + `channel_ebill`
relationship. `deliverable.data.type` ∈ letters|emails|ebills.

**Error body**: `errors[] { code?, title?, detail?, source { pointer?, parameter? }? }` — all
nullable strings.

---

## §9 Implementation phases & sub-agent briefs

Every phase: dispatch sub-agent(s) → gate (`dotnet build` + `dotnet test` green) → commit (§12.2)
→ push. Sub-agents always read `CLAUDE.md` + the listed plan sections. Phase order respects
compile-time dependencies; ⫲ marks phases that may run in parallel with the previous one.

| Phase | Scope (files per §5) | Plan sections | Notes |
|---|---|---|---|
| **0** | Orchestrator does directly: (a) verify the .NET 10 SDK — `dotnet --version`; if missing, install it first (`dotnet-install.sh --channel 10.0` into `$HOME/.dotnet`, export `PATH`/`DOTNET_ROOT`, mind any HTTPS proxy); (b) add a `.tmp/` entry to `.gitignore` (`main` lacks it) so this plan and the spec can never be committed; (c) create `CLAUDE.md` from Appendix A verbatim; (d) add package refs (§2 table); (e) migrate the test csproj to xunit.v3: swap `xunit` → `xunit.v3`, **add `<OutputType>Exe</OutputType>`** (v3 test projects are executables), keep `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` and `coverlet.collector` as-is (VSTest mode); (f) `dotnet build` gate. | §2 | 1 commit |
| **1** | `Common/**` (Json, JsonApi, PingenList, PingenListOptions, PingenFilter, PingenRequestOptions, PingenError, PingenException) + unit tests | §4, §6.2 (query builder + PingenFilter), §8 (error body), §10 | The serialization bedrock. Include the DateTimeOffset converter edge cases in tests. |
| **2** | `Options/**`, `Authentication/**`, `PingenConfiguration.cs` (everything except service registrations), `PingenClient.cs` (request core only — no hub properties yet) **plus the shared test infra `Tests/RecordingHandler.cs` + `Tests/PingenTestHost.cs`** + tests | §3, §4 (auth/hosts/headers), §6.1 incl. ownership note, §6.2, §10 | Token caching, 401-retry-once, AllowAutoRedirect=false, DI binding test. |
| **3** | `Files/**`, `Deliveries/ValueTypes/**`, `Deliveries/DeliverableEvent.cs` + tests | §4 (file flows), §6.3, §8 | Unblocks all create flows. |
| **4** | `Deliveries/Letters/**` + tests | §4, §6.3, §7 rows 6–19, §8 | Largest single service. |
| **5** ⫲ | `Deliveries/Emails/**`, `Deliveries/Ebills/**` + tests | §6.3, §7 rows 20–36, §8 | Parallel with 4 after 3 (both only depend on 1–3). If parallel, give each agent disjoint files. |
| **6** | `Batches/**` + tests | §6.3, §7 rows 37–45, §8 | DELETE-with-body, oneOf send options. |
| **7** ⫲ | `Organisations/**`, `Users/**`, `Webhooks/**` (incl. payloads + PingenWebhook) + tests | §4 (webhook security), §6.3, §7 rows 2–5 + 46–49, §8 | Parallel with 6. |
| **8** | Sole owner of `PingenClient.cs`/`PingenConfiguration.cs` wiring: add all eight hub properties + all eight `AddTransient<…Service>()` registrations, plus the §10 item 11 resolution test; then a full-solution review agent: XML-doc gaps, style drift, coverage matrix check against §7 | §5, §6, §7 | Review agent reports issues; fix agent applies. |
| **9** | `README.md` at repo root | §11 | Then `dotnet pack -c Release` must succeed as gate. |
| **10** | Final gate: `dotnet build -c Release`, `dotnet test -c Release`, `dotnet pack`; tally check; final push. | — | No new code. |

**Brief template** (adapt per phase):

```
You are implementing phase N of Pingen.Client. Read, in this order:
  1. CLAUDE.md (repo root) — binding style rules
  2. .tmp/IMPLEMENTATION_PLAN.md sections §4, §<...> — your contract
Create exactly these files: <list from §5>.
Definition of done: files exist, `dotnet build src/Pingen.Client/Pingen.Client.sln` and
`dotnet test` pass including your new tests listed in §10 for this phase.
Do not touch files outside your list. Do not read .tmp/swagger-docs.json unless a wire
detail is genuinely missing from §8 — then use Appendix B to slice out only that schema.
Report back ONLY: files created, public types added (names), gate status, deviations. Max 25 lines.
```

---

## §10 Testing strategy

xunit.v3 + FluentAssertions `[7.2.0]`, test project mirrors source folders
(`Pingen.Client.Tests/Deliveries/Letters/LetterServiceTests.cs`, …). House conventions (also in
Appendix A): test names `When_<condition>_<Method>_<expected>` / `Given_…_When_…_The_…`; the only
comments inside tests are `// Arrange`, `// Act`, `// Assert`; `[Theory]`/`[InlineData]` for value
tables; `TestContext.Current.CancellationToken` where a token is passed; prefer fewer, denser
tests; never wrap a bug in a green test.

Shared infra in `Pingen.Client.Tests/Tests/` (owned by phase 2 — later phases only consume it):

- `RecordingHandler : HttpMessageHandler` — captures each `HttpRequestMessage` (method, URL,
  headers, body string) and returns queued canned responses (status + body + headers). This is the
  house-analog of integration-first testing for an HTTP library — no NSubstitute for HttpClient.
- `PingenTestHost` — builds a real `ServiceCollection`, calls `AddPingen(o => …)`, swaps the
  primary handlers of all three clients for `RecordingHandler`s (a later
  `ConfigurePrimaryHttpMessageHandler` on the same named/typed client wins over the one AddPingen
  registered — the builder actions run in order and the last primary-handler assignment sticks),
  pre-queues a token response on the identity handler, and exposes the provider, the three
  recorders, and `PingenClient`. Until phase 8 wires service registrations, phases 3–7 resolve
  `PingenClient` from the host and construct their service under test from it
  (`new LetterService(host.Client)`) — the §10 item 11 resolution test lands with phase 8.
- Canned JSON:API fixtures as C# raw string literals colocated with the tests that use them, built
  from the §8 field lists (realistic values, e.g. timestamps with `+0100`).

Minimum coverage per area (write these; add more where a bug seems plausible). Ownership: items
1–2 → phase 1 (except enum wire names of types that don't exist yet — each later phase tests its
own enums' wire names on arrival); 3 → phase 2; 4–6 → every service phase (3–7) for its §7 rows;
7 → phases 3/4 (upload flow via Letters); 8 → phase 6; 9 → phase 7; 10 → first list-bearing phase
(4) then reused; 11 → phase 8.

1. **Json/converters**: `+0100` and `+01:00` offsets parse; empty/missing `submitted_at` → null;
   DateOnly round-trip; per-member enum wire names (phase 6 adds `wave-hand`/`qr_invoice`/
   `electronic_email` cases with BatchIcon); request serialization omits nulls; response with
   unknown extra fields deserializes.
2. **Query building**: `page[number]`/`page[limit]`, sort with `-` prefix, `fields[letters]`,
   `include`, `language`, escaping; `PingenFilter.And(Where("status","sent"), GreaterThan("created_at", d))`
   produces the exact JSON grammar of §4.
3. **Auth**: token fetched once for two calls (cache); expiry triggers refetch; 401 → invalidate +
   retry once then throw; form fields of the token request; token request goes to identity host.
4. **Request construction per service** (the bulk): every §7 row gets at least one test asserting
   method, exact path, query, `Content-Type: application/vnd.api+json`, envelope shape
   (`data.type`, `data.id` present on send/edit/delete-body PATCHes, absent on creates), and
   response mapping (attributes land, `Meta.Abilities` populated on single, null on list items).
5. **Error mapping**: 422 body → `PingenException` with parsed errors + `RequestId`; 429 →
   `RetryAfter` set; non-JSON error body → still throws with status.
6. **302 flows**: `GetFileLocationAsync` does not follow redirect, returns Location;
   `DownloadFileAsync` second request has **no Authorization header**.
7. **Upload flow**: `CreateAsync(Stream, …)` performs GET /file-upload → PUT (raw body, no auth,
   no multipart) → POST with copied `file_url`/`file_url_signature`; PUT body bytes match input.
8. **Batch specials**: DELETE carries required JSON body; `BatchSendOptions.Post/Email/Ebill`
   serialize the three distinct `data.type` values; edit PATCH carries `data.id`.
9. **Webhooks**: `VerifySignature` accepts the correct HMAC hex, rejects tampered payload and wrong
   key; `ConstructEvent` returns the right derived type for all five payloads; unknown type throws;
   byte-exact payload verification (include a unicode char in the fixture).
10. **Auto-paging**: two-page fixture enumerates all items, requests page 2 with the same options,
    stops at last page.
11. **Options/DI**: `AddPingen()` binds a `"Pingen"` config section; missing ClientId fails
    ValidateOnStart; all eight services + `PingenClient` resolve.

---

## §11 README requirements

Root `README.md` (packed into the NuGet package — keep it concise, skimmable, .NET-native).
Sections, in order:

1. Title + one-liner, sourced from the csproj metadata (".NET client for the Pingen API", by
   weboost.at, MIT) — do not editorialize about official/unofficial status; omit badges until the
   first publish.
2. **Install**: `dotnet add package Pingen.Client`.
3. **Quickstart**: `services.AddPingen();` + `appsettings.json` `"Pingen": { "ClientId": …,
   "ClientSecret": …, "Environment": "Staging" }` + a 6-line send-a-letter sample using
   `client.Letters.CreateAsync(orgId, pdfStream, new LetterCreateOptions { … AutoSend = true … })`.
4. **Configuration** table: every `PingenOptions` property, default, meaning; code-based
   `AddPingen(o => …)` variant; note on scopes incl. the `user` scope quirk.
5. **Concepts**: one short paragraph each — organisation-scoped calls; JSON:API shape
   (`letter.Attributes.Status`); `PingenList<T>` + `ListAutoPagingAsync`; `PingenListOptions`
   (paging/sort/`PingenFilter`/search/fields); `PingenRequestOptions.IdempotencyKey`;
   `PingenException`.
6. **Files**: the 3-step upload done for you (Stream overloads) + manual `Files` usage; downloads
   (`DownloadFileAsync`, presigned URLs, no auth).
7. **Webhooks**: register via `Webhooks.CreateAsync`; verify + parse inbound with
   `PingenWebhook.ConstructEvent` — minimal ASP.NET endpoint sample reading the raw body.
8. **Staging & simulation**: staging host selection + the `*_simulate_*.pdf` filename table.
9. **License** (MIT) + link to official Pingen API docs.

No architecture essays, no changelog, no contributing section (not requested).

---

## §12 House style digest & commit conventions

### 12.1 Code style — non-negotiables

The complete rules live in Appendix A (→ `CLAUDE.md`); headline items every sub-agent must
internalize: file-scoped namespaces = folder path; one concept per file (satellite types co-locate);
records by default, classes only for behavior/mutable state; primary constructors, params used
directly (no field copies unless derived); NO `sealed`, NO `internal` types (public or private
only — exception: the request core on `PingenClient` is `internal` by necessity of the design; keep
internal members to that one class), no `#region`, no interfaces for services; `var` everywhere;
expression bodies for single-expression members; brace-less one-line guards; `is null` / `is not
null`; switch expressions; collection expressions `[]`; target-typed `new`; named arguments on
multi-arg constructions; `_camelCase` private fields, PascalCase consts; C# 14 `extension(...)`
blocks for public extension members (classic `this` allowed for DI-config extensions and private
helpers); `Async` suffix; `CancellationToken cancellationToken = default` last (library deviation
from the app codebase — deliberate); no `ConfigureAwait` (matches house style — zero uses in
outreach; the surface is async-only, consumers who block on it own the deadlock risk); XML
`<summary>` one-liners on all public API — never on private members; comments are one-line, state
a *why* or a hazard, never narrate the next line; dashes as comment punctuation. Plus the house
code-quality trio: don't defend against states that can't occur; don't duplicate — promote to
`Common/`; inline what's used once.

### 12.2 Commit messages — exact grammar

`<PastTenseVerb> <FileName.ext>[, <File2.ext> and <File3.ext>], <lowercase present-tense behavior clause>[, <clause>…]`

- Verbs: `Added`, `Updated`, `Fixed`, `Refactored`, `Moved`, `Dropped`. No trailing period. No
  conventional-commit prefixes. Backtick code identifiers inside the clause. Comma-splice multiple
  facts; rationale with "so"/"since". For folder-sized changes name the anchor files, not all of them.
  For pure small additions the attested short form is also fine: `Added <File.cs> <role noun>`
  (e.g. `Added FileUpload.cs resource`).
- Examples for this project:
  - `Added PingenJson.cs and PingenDateTimeConverter.cs, timestamps parse the +0100 offsets the API emits`
  - `Added LetterService.cs, letters cover list, create, send, cancel and the price calculator, the stream overload runs the 3-step upload`
  - `Added PingenWebhook.cs, signatures verify constant-time against the raw payload bytes`
  - `Updated PingenConfiguration.cs, the files client skips the auth handler since presigned URLs reject Bearer headers`
- Keep the session's mandated trailers (Co-Authored-By etc.) if your harness requires them.
- Cadence: commit and push per phase as §1 rule 7 requires — the history should read like the
  owner built the library feature by feature, not like one code drop.
- Branches: when the session doesn't dictate one, `main` directly or a single house-style
  `feature/<topic>` branch (attested in outreach: `feature/products`) for the entire implementation.

---

## §13 Decision log

| Decision | Choice | Why |
|---|---|---|
| Interfaces for services | None — concrete classes | House rule (zero service interfaces in outreach); consumers stub via `BaseAddress` + test server, mirroring the lead dev's integration-first testing. Revisit (`public virtual` à la Stripe) only if the owner asks. |
| Sync method variants | No — async only | Stripe's sync surface is legacy back-compat; modern .NET + house style are async-first. |
| FluentValidation for options | No — `IValidateOptions<T>` hand-rolled | House pattern uses FluentValidation, but forcing a third-party dependency on every consumer of a client library is a real cost; the validator file/shape still mirrors the house `<X>OptionsValidator` convention. |
| Response enums | Strings for everything read-only | Pingen documents statuses/codes as open sets; Stripe made the same call. Enums only where the SDK writes values. |
| Flatten attributes into entity (Stripe-flat) | No — keep `Attributes` nesting | Honest to JSON:API, mechanical to implement/test, keeps relationships/meta/abilities addressable; flattening 8 resources is pure ceremony. |
| Auto-generated idempotency keys | No | Pingen keys are 24h-scoped and replay full responses; silent auto-keys can mask duplicate-send bugs. Explicit opt-in via `PingenRequestOptions`. |
| Automatic 429/503 retries | No (v1) | Keep the transport predictable; `RetryAfter` is surfaced, consumers can wrap with Polly. Documented in README as a non-goal. |
| `record` vs `class` for options/entities | Records with `{ get; init; }` (+ `required` where wire-required) | House style: records for contracts; `required` mirrors spec required lists. |
| Multi-targeting (netstandard2.0 etc.) | net10.0 only | The repo owner configured net10.0; "agnostic" means framework-agnostic, not TFM breadth. Trivial to widen later. |
| Default organisation id in options | Not in v1 | Explicit `Guid organisationId` params keep parity with the API; a convenience default can be added without breaking. |
| Price calculator empty 202 | `Task<LetterPrice?>`, null on 202 | Spec declares both 200+body and bodyless 202; null is the only honest mapping. |
| `Ebill` casing | `Ebill` (not `EBill`) | Matches wire type `ebills` and reads as one word, consistent with `EbillService` file naming. |
| `ConfigureAwait(false)` | Not used | Matches house style (zero occurrences in outreach). Deviates from generic NuGet-library guidance; the async-only surface means consumers who sync-block accept that risk knowingly. |
| `sealed` on wire contracts | Not used | Outreach's written rule ("No sealed") wins over its one sealed precedent (the AustrianCompanyRecord.cs contract-mirror outlier). |

---

## Appendix A — CLAUDE.md to create at repo root (phase 0, verbatim)

```markdown
# Pingen.Client — house rules

.NET client library for the Pingen v2 API. net10.0, nullable enabled, DI-native, no framework coupling.
Full architecture and wire contracts: .tmp/IMPLEMENTATION_PLAN.md (gitignored working document — never commit it; sections are numbered, read what your task names).

## Style (binding)

- File-scoped namespaces; namespace = folder path rooted at `Pingen.Client`.
- One concept per file, named after its central type; closely-coupled satellite types live in the
  same file (an options record's nested section, a service's small response records).
- Records by default (`{ get; init; }`, `required` for wire-required members); classes only where
  behavior or mutable state lives; primary constructors, use parameters directly.
- No `sealed`, no `#region`, no service interfaces, no ceremony. Public or private — avoid
  `internal` except the request core on `PingenClient`.
- `var` for locals; expression bodies for any single-expression member; brace-less one-line guards
  (`if (x is null) return …;`); `is null` / `is not null` — never `== null`; switch expressions
  with a `_` arm; collection expressions (`[]`, `[item]`, spreads); target-typed `new`; named
  arguments on multi-argument constructions, one per line, closing `)` on its own line.
- `_camelCase` private fields; PascalCase consts and static readonly fields.
- Extension members: C# 14 `extension(...)` blocks for public extensions; classic `this` parameters
  only for `IServiceCollection`-style config extensions and private helpers.
- Async: `Async` suffix on every async method; `CancellationToken cancellationToken = default` as
  the last parameter of every public async API; `await using` for async disposables; no
  `.Result`/`.Wait()`; no `ConfigureAwait` (net10.0-only library, no sync-context consumers).
- JSON: explicit `[JsonPropertyName("snake_case")]` on every wire property — never rely on a naming
  policy; `JsonSerializerOptions` cached in `private static readonly` fields; System.Text.Json only.
- XML docs: one-sentence `<summary>` on every public type and member — NEVER on private members or
  the internal request core; document defaults on options (`default is <c>587</c>` style); no
  `<param>` on record positional parameters, no `<remarks>` essays.
- Comments: one-liners that state a constraint, hazard, or why — never what the next line does.
  Dash punctuation ("Constant-time - == leaks…"). Comment as a last resort.
- Don't defend against states that can't occur — a null check no call path can trigger is
  misinformation, not safety. Don't duplicate — promote shared helpers to `Common/`. Inline
  what's used once.

## Tests (binding)

- xunit.v3 + FluentAssertions 7 (`Should()`); test files mirror source folders; names
  `When_<condition>_<Method>_<expected>` or `Given_…_When_…_It_…` in underscore form.
- Only `// Arrange`, `// Act`, `// Assert` comments inside tests. `[Theory]`/`[InlineData]` for
  value tables (include unicode/edge values). `TestContext.Current.CancellationToken` when passing tokens.
- Tests go through real DI via the shared `PingenTestHost` + `RecordingHandler` (Tests/ folder);
  never mock HttpClient with a mocking framework. Until phase 8 wires service registrations,
  construct the service under test from the DI-resolved client (`new LetterService(host.Client)`).
- Prefer fewer, denser tests; shared setup in test bases; never wrap a bug in a green test.

## Commits

`<PastTenseVerb> <File.ext>[, <File2.ext> and <File3.ext>], <lowercase behavior clause>` — verbs:
Added/Updated/Fixed/Refactored/Moved/Dropped; backticked identifiers; rationale with "so"/"since";
no trailing period. Example: `Added PingenAccessTokens.cs, tokens cache for 12 hours and refresh 60 seconds early`

Commit and push after every coherent unit of work (a phase) — never batch the implementation into
one drop. If a branch is used at all, it is a single `feature/<topic>` branch for the whole job.

## Boundaries

- Never edit `.github/workflows/**`, never commit `.tmp/**`, never add dependencies beyond the set
  in .tmp/IMPLEMENTATION_PLAN.md §2.
- `README.md` at repo root is packed into the NuGet package — `dotnet pack` fails without it.
```

## Appendix B — spec extraction escape hatch (sub-agents only)

The spec is `.tmp/swagger-docs.json` (OpenAPI 3.0, 5.5 MB — never read it whole). To inspect one
operation or schema:

```bash
python3 - <<'EOF'
import json
d = json.load(open('.tmp/swagger-docs.json'))
# one operation:
print(json.dumps(d['paths']['/organisations/{organisationId}/deliveries/letters']['post'], indent=1)[:6000])
# one schema (resolve $ref targets under components.schemas / components.parameters):
print(json.dumps(d['components']['schemas']['LetterCreatePOST'], indent=1)[:6000])
EOF
```

Resolve `$ref`s manually by following the path inside the same document. Extract the minimum,
never dump whole sections into context.

