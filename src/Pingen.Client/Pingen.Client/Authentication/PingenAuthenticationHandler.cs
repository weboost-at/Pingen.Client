using System.Net;

namespace Pingen.Client.Authentication;

/// <summary>
/// Attaches the cached bearer token to every API request and replays the request once when Pingen rejects the token.
/// </summary>
public class PingenAuthenticationHandler(PingenAccessTokens tokens) : DelegatingHandler
{
    /// <summary>
    /// Sends <paramref name="request"/> authenticated, retrying it once with a fresh token after a 401.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new("Bearer", await tokens.GetAsync(cancellationToken));

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode is not HttpStatusCode.Unauthorized) return response;

        // A 401 means the token went stale before the request landed - drop it and replay once, a second 401 is a credentials problem.
        response.Dispose();
        tokens.Invalidate();

        using var retry = await CloneAsync(request, cancellationToken);
        retry.Headers.Authorization = new("Bearer", await tokens.GetAsync(cancellationToken));
        return await base.SendAsync(retry, cancellationToken);
    }

    // A request message that has been sent cannot be sent again - the replay goes out as a copy.
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var (name, values) in request.Headers) clone.Headers.TryAddWithoutValidation(name, values);
        if (request.Content is null) return clone;

        clone.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
        foreach (var (name, values) in request.Content.Headers) clone.Content.Headers.TryAddWithoutValidation(name, values);

        return clone;
    }
}
