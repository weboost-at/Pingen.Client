using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Pingen.Client.Batches;
using Pingen.Client.Common;
using Pingen.Client.Common.Json;
using Pingen.Client.Deliveries.Ebills;
using Pingen.Client.Deliveries.Emails;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Files;
using Pingen.Client.Organisations;
using Pingen.Client.Users;
using Pingen.Client.Webhooks;

// The request core below is the one internal surface of the library, and the tests drive it directly.
[assembly: InternalsVisibleTo("Pingen.Client.Tests")]

namespace Pingen.Client;

/// <summary>The Pingen API client - resolve it from dependency injection and reach the resources through its services.</summary>
public class PingenClient(HttpClient httpClient, IHttpClientFactory httpClientFactory)
{
    /// <summary>The name of the HTTP client talking to the identity host, registered without the authentication handler.</summary>
    public const string IdentityClientName = "PingenIdentity";

    /// <summary>The name of the HTTP client talking to presigned file URLs, registered without the authentication handler since a bearer token invalidates a presigned signature.</summary>
    public const string FilesClientName = "PingenFiles";

    /// <summary>The media type every JSON body of the Pingen API is sent and returned as.</summary>
    public const string JsonApiMediaType = "application/vnd.api+json";

    private static readonly MediaTypeHeaderValue JsonApiContentType = new(JsonApiMediaType);

    private LetterService? _letters;
    private EmailService? _emails;
    private EbillService? _ebills;
    private BatchService? _batches;
    private OrganisationService? _organisations;
    private UserService? _users;
    private WebhookService? _webhooks;
    private FileService? _files;

    /// <summary>The letters of an organisation - physical mail Pingen prints, franks and hands to a postal service.</summary>
    public LetterService Letters => _letters ??= new(this);

    /// <summary>The email channel, which delivers a document as an email instead of printing it.</summary>
    public EmailService Emails => _emails ??= new(this);

    /// <summary>The ebill channel, which delivers an invoice into the recipient's e-banking.</summary>
    public EbillService Ebills => _ebills ??= new(this);

    /// <summary>The batches of an organisation - one upload split into many deliveries, dispatched through one channel.</summary>
    public BatchService Batches => _batches ??= new(this);

    /// <summary>The organisations the authenticated user may act for.</summary>
    public OrganisationService Organisations => _organisations ??= new(this);

    /// <summary>The user the access token was issued for and the organisations it is associated with.</summary>
    public UserService Users => _users ??= new(this);

    /// <summary>The webhook subscriptions of an organisation.</summary>
    public WebhookService Webhooks => _webhooks ??= new(this);

    /// <summary>The file transfer half of the API - presigned upload targets, raw uploads and downloads.</summary>
    public FileService Files => _files ??= new(this);

    internal HttpClient FileClient => httpClientFactory.CreateClient(FilesClientName);

    internal Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) =>
        GetAsync<T>(path, null, cancellationToken);

    internal Task<T> GetAsync<T>(string path, PingenListOptions? listOptions, CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Get, path + PingenQuery.Build(listOptions), null, null, cancellationToken);

    internal async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, PingenRequestOptions? requestOptions, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, path, body, requestOptions, cancellationToken);
        var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        // A caller asking for T was promised a resource - handing back a null through that non-nullable return would only fail further from here.
        if (payload.Length is 0) throw Unanswered(method, path, response);

        return JsonSerializer.Deserialize<T>(payload, PingenJson.Options)!;
    }

    // Accepted answers such as the price calculator's carry no body at all - the endpoints that may skip the payload ask for it through this method.
    internal async Task<T?> SendOrDefaultAsync<T>(HttpMethod method, string path, object? body, PingenRequestOptions? requestOptions, CancellationToken cancellationToken)
        where T : class
    {
        using var response = await SendCoreAsync(method, path, body, requestOptions, cancellationToken);
        var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        return payload.Length is 0 ? null : JsonSerializer.Deserialize<T>(payload, PingenJson.Options);
    }

    internal async Task SendAsync(HttpMethod method, string path, object? body, PingenRequestOptions? requestOptions, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, path, body, requestOptions, cancellationToken);
    }

    internal async Task<Uri> GetLocationAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        // File endpoints answer 302 with an empty body and the presigned URL in the header - redirects are not followed, the header is the payload.
        if (response.Headers.Location is { } location) return location;

        throw await ToExceptionAsync(response, cancellationToken);
    }

    internal static async Task<PingenException> ToExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        new(
            statusCode: response.StatusCode,
            errors: await ReadErrorsAsync(response, cancellationToken),
            requestId: RequestId(response),
            retryAfter: response.Headers.RetryAfter?.Delta
        );

    private static PingenException Unanswered(HttpMethod method, string path, HttpResponseMessage response) =>
        new(
            statusCode: response.StatusCode,
            errors: [new() { Title = "The response carried no payload", Detail = $"{method} {path} answered {(int)response.StatusCode} with an empty body where a resource was expected." }],
            requestId: RequestId(response)
        );

    private static string? RequestId(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Request-Id", out var requestIds) ? requestIds.FirstOrDefault() : null;

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, object? body, PingenRequestOptions? requestOptions, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), JsonApiContentType, PingenJson.Options);
        if (requestOptions?.IdempotencyKey is { Length: > 0 } key) request.Headers.Add("Idempotency-Key", key);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return response;

        using (response) throw await ToExceptionAsync(response, cancellationToken);
    }

    private static async Task<IReadOnlyList<PingenError>> ReadErrorsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var document = await response.Content.ReadFromJsonAsync<PingenErrorDocument>(PingenJson.Options, cancellationToken);
            return document?.Errors ?? [];
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Maintenance pages and gateway errors answer with HTML - the status code is then all there is to report.
            return [];
        }
    }
}
