using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Pingen.Client.Common.Json;
using Pingen.Client.Options;

namespace Pingen.Client.Authentication;

/// <summary>Holds the client-credentials access token every Pingen request shares, fetching a new one whenever the cached one is spent.</summary>
public class PingenAccessTokens(IHttpClientFactory httpClientFactory, IOptions<PingenOptions> options)
{
    private readonly SemaphoreSlim _gate = new(initialCount: 1, maxCount: 1);
    private AccessToken? _token;

    /// <summary>Returns the cached token, requesting one from the identity host when none is cached or the cached one expired.</summary>
    public async Task<string> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_token is { IsExpired: false } cached) return cached.Value;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_token is { IsExpired: false } current) return current.Value;

            _token = await RequestAsync(cancellationToken);
            return _token.Value;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Drops the cached token so the next request fetches a fresh one - the answer to a 401.</summary>
    public void Invalidate() => _token = null;

    private async Task<AccessToken> RequestAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        Dictionary<string, string> fields = new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = settings.ClientId!,
            ["client_secret"] = settings.ClientSecret!,
        };
        if (settings.Scopes is { Length: > 0 } scopes) fields["scope"] = scopes;

        var client = httpClientFactory.CreateClient(PingenClient.IdentityClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/access-tokens") { Content = new FormUrlEncodedContent(fields) };
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await PingenClient.ToExceptionAsync(response, cancellationToken);

        var issued = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(PingenJson.Options, cancellationToken);
        return new(issued!.AccessToken, DateTimeOffset.UtcNow.AddSeconds(issued.ExpiresIn));
    }
}
