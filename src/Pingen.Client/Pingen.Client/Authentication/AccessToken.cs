using System.Text.Json.Serialization;

namespace Pingen.Client.Authentication;

/// <summary>A bearer token issued by the Pingen identity host together with the moment it stops being valid.</summary>
public record AccessToken(string Value, DateTimeOffset ExpiresAt)
{
    // Renewed a minute early so a token cannot expire between being read and reaching the API.
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromSeconds(60);

    /// <summary>Whether the token has reached its refresh point and must be replaced.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt - RefreshWindow;
}

/// <summary>The body the Pingen identity host answers a client-credentials request with.</summary>
public record AccessTokenResponse
{
    /// <summary>The token scheme, always <c>Bearer</c>.</summary>
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    /// <summary>The lifetime of the token in seconds, 43200 for the 12 hour tokens Pingen issues.</summary>
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }

    /// <summary>The token to send as <c>Authorization: Bearer</c>.</summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }
}
