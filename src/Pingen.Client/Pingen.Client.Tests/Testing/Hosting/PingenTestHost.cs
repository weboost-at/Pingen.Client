using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Pingen.Client.Options;

namespace Pingen.Client.Tests.Testing.Hosting;

// A real container wired through AddPingen, with the API, the identity host and presigned URLs answered by recorders
public class PingenTestHost : IDisposable
{
    public const string ClientId = "test-client-id";

    public const string ClientSecret = "test-client-secret";

    public const string AccessToken = "test-access-token";

    public PingenTestHost(Action<PingenOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddPingen(options =>
        {
            options.ClientId = ClientId;
            options.ClientSecret = ClientSecret;
            configure?.Invoke(options);
        });

        // A primary handler configured after AddPingen wins - the builder actions run in order and the last assignment sticks.
        services.AddHttpClient<PingenClient>().ConfigurePrimaryHttpMessageHandler(() => Api);
        services.AddHttpClient(PingenClient.IdentityClientName).ConfigurePrimaryHttpMessageHandler(() => Identity);
        services.AddHttpClient(PingenClient.FilesClientName).ConfigurePrimaryHttpMessageHandler(() => Files);

        Provider = services.BuildServiceProvider();
        EnqueueToken();
    }

    public TestClock Clock { get; } = new();

    public RecordingHandler Api { get; } = new();

    public RecordingHandler Identity { get; } = new();

    public RecordingHandler Files { get; } = new();

    public ServiceProvider Provider { get; }

    public PingenClient Client => Provider.GetRequiredService<PingenClient>();

    // Every host starts with one valid token queued, a test queues more for the refreshes it provokes
    public PingenTestHost EnqueueToken(string accessToken = AccessToken, int expiresIn = 43200)
    {
        Identity.EnqueueJson(
            HttpStatusCode.OK,
            $$"""{"token_type":"Bearer","expires_in":{{expiresIn}},"access_token":"{{accessToken}}"}""",
            "application/json"
        );

        return this;
    }

    public void Dispose() => Provider.Dispose();
}
