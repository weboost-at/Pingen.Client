using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pingen.Client.Authentication;
using Pingen.Client.Options;

namespace Pingen.Client;

/// <summary>Registers the Pingen client with a dependency injection container.</summary>
public static class PingenConfiguration
{
    /// <summary>Registers the Pingen client, binding options from the <c>Pingen</c> configuration section.</summary>
    public static IServiceCollection AddPingen(this IServiceCollection services)
    {
        services.AddOptions<PingenOptions>().BindConfiguration("Pingen").ValidateOnStart();

        return AddPingenServices(services);
    }

    /// <summary>Registers the Pingen client with options configured in code.</summary>
    public static IServiceCollection AddPingen(this IServiceCollection services, Action<PingenOptions> configure)
    {
        services.AddOptions<PingenOptions>().Configure(configure).ValidateOnStart();

        return AddPingenServices(services);
    }

    private static IServiceCollection AddPingenServices(IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<PingenOptions>, PingenOptionsValidator>();
        services.AddSingleton<PingenAccessTokens>();
        services.AddTransient<PingenAuthenticationHandler>();

        services.AddHttpClient(PingenClient.IdentityClientName, (provider, client) =>
        {
            var options = Settings(provider);
            client.BaseAddress = options.IdentityAddress ?? options.Environment.IdentityAddress;
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));
        });

        // Presigned URLs carry their own signature and reject an Authorization header - this client stays untouched.
        services.AddHttpClient(PingenClient.FilesClientName);

        services.AddHttpClient<PingenClient>((provider, client) =>
            {
                var options = Settings(provider);
                client.BaseAddress = options.BaseAddress ?? options.Environment.ApiAddress;
                client.DefaultRequestHeaders.Accept.Add(new(PingenClient.JsonApiMediaType));
            })
            // The file endpoints answer 302 with the presigned URL in the Location header, which must reach the caller instead of being followed.
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .AddHttpMessageHandler<PingenAuthenticationHandler>();

        return services;
    }

    private static PingenOptions Settings(IServiceProvider provider) => provider.GetRequiredService<IOptions<PingenOptions>>().Value;
}
