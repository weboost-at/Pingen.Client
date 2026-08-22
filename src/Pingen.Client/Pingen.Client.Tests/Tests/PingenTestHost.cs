using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Pingen.Client.Options;

namespace Pingen.Client.Tests.Tests;

/// <summary>
/// A real dependency injection container wired through <c>AddPingen</c>, with all three HTTP clients answered by
/// recorders.
/// </summary>
public class PingenTestHost : IDisposable
{
    /// <summary>
    /// The access token the pre-queued identity response hands out.
    /// </summary>
    public const string AccessToken = "test-access-token";

    /// <summary>
    /// Builds the host, applying <paramref name="configure"/> on top of working credentials.
    /// </summary>
    public PingenTestHost(Action<PingenOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddPingen(options =>
        {
            options.ClientId = "test-client-id";
            options.ClientSecret = "test-client-secret";
            configure?.Invoke(options);
        });

        // A primary handler configured after AddPingen wins - the builder actions run in order and the last assignment sticks.
        services.AddHttpClient<PingenClient>().ConfigurePrimaryHttpMessageHandler(() => Api);
        services.AddHttpClient(PingenClient.IdentityClientName).ConfigurePrimaryHttpMessageHandler(() => Identity);
        services.AddHttpClient(PingenClient.FilesClientName).ConfigurePrimaryHttpMessageHandler(() => Files);

        Provider = services.BuildServiceProvider();
        EnqueueToken();
    }

    /// <summary>
    /// The recorder answering the API host.
    /// </summary>
    public RecordingHandler Api { get; } = new();

    /// <summary>
    /// The recorder answering the identity host.
    /// </summary>
    public RecordingHandler Identity { get; } = new();

    /// <summary>
    /// The recorder answering presigned file URLs.
    /// </summary>
    public RecordingHandler Files { get; } = new();

    /// <summary>
    /// The container every service under test is resolved from.
    /// </summary>
    public ServiceProvider Provider { get; }

    /// <summary>
    /// The client under test, resolved through the typed client registration.
    /// </summary>
    public PingenClient Client => Provider.GetRequiredService<PingenClient>();

    /// <summary>
    /// Queues another identity answer handing out an access token, by default a valid one.
    /// </summary>
    public PingenTestHost EnqueueToken(string accessToken = AccessToken, int expiresIn = 43200)
    {
        Identity.EnqueueJson(
            HttpStatusCode.OK,
            $$"""{"token_type":"Bearer","expires_in":{{expiresIn}},"access_token":"{{accessToken}}"}""",
            "application/json"
        );

        return this;
    }

    /// <summary>
    /// Disposes the container.
    /// </summary>
    public void Dispose() => Provider.Dispose();
}
