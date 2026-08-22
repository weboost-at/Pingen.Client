using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Pingen.Client.Common;
using Pingen.Client.Common.Json;

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

    internal HttpClient FileClient => httpClientFactory.CreateClient(FilesClientName);

    internal Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) =>
        GetAsync<T>(path, null, cancellationToken);

    internal Task<T> GetAsync<T>(string path, PingenListOptions? listOptions, CancellationToken cancellationToken) =>
        SendAsync<T>(HttpMethod.Get, path + PingenQuery.Build(listOptions), null, null, cancellationToken);

    internal async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, PingenRequestOptions? requestOptions, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(method, path, body, requestOptions, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<T>(PingenJson.Options, cancellationToken))!;
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
            requestId: response.Headers.TryGetValues("X-Request-Id", out var requestIds) ? requestIds.FirstOrDefault() : null,
            retryAfter: response.Headers.RetryAfter?.Delta
        );

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
