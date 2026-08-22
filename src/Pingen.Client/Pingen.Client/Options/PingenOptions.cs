namespace Pingen.Client.Options;

/// <summary>Credentials and hosts the Pingen client is configured with, bound from the <c>Pingen</c> configuration section.</summary>
public class PingenOptions
{
    /// <summary>The OAuth client id of the Pingen API client.</summary>
    public string? ClientId { get; set; }

    /// <summary>The OAuth client secret of the Pingen API client.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>The deployment to talk to - default is <c>Production</c>.</summary>
    public PingenEnvironment Environment { get; set; } = PingenEnvironment.Production;

    /// <summary>The space-separated scopes to request, for example <c>letter webhook user</c> - default is <c>null</c>, which asks for every scope the client is registered for.</summary>
    public string? Scopes { get; set; }

    /// <summary>The API host, overriding the default of <see cref="Environment"/> - default is <c>null</c>.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>The identity host issuing access tokens, overriding the default of <see cref="Environment"/> - default is <c>null</c>.</summary>
    public Uri? IdentityAddress { get; set; }
}
