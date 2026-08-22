using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pingen.Client.Authentication;
using Pingen.Client.Options;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests;

public class PingenConfigurationTests
{
    [Fact]
    public void When_a_Pingen_section_is_present_AddPingen_binds_every_option_from_it()
    {
        // Arrange
        var services = Configured(new()
        {
            ["Pingen:ClientId"] = "bound-id",
            ["Pingen:ClientSecret"] = "bound-secret",
            ["Pingen:Environment"] = "Staging",
            ["Pingen:Scopes"] = "letter user",
            ["Pingen:BaseAddress"] = "https://api.example.com/",
            ["Pingen:IdentityAddress"] = "https://identity.example.com/",
        });

        // Act
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PingenOptions>>().Value;

        // Assert
        options.ClientId.Should().Be("bound-id");
        options.ClientSecret.Should().Be("bound-secret");
        options.Environment.Should().Be(PingenEnvironment.Staging);
        options.Scopes.Should().Be("letter user");
        options.BaseAddress.Should().Be(new Uri("https://api.example.com/"));
        options.IdentityAddress.Should().Be(new Uri("https://identity.example.com/"));
    }

    [Fact]
    public void When_the_section_omits_the_client_id_the_startup_validation_fails()
    {
        // Arrange
        var services = Configured(new() { ["Pingen:ClientSecret"] = "bound-secret" });

        // Act
        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        act.Should().Throw<OptionsValidationException>().WithMessage("*ClientId is required.*");
    }

    [Fact]
    public void When_the_container_is_built_AddPingen_registers_the_client_and_its_authentication_parts()
    {
        // Arrange
        using var host = new PingenTestHost();

        // Act
        var client = host.Client;

        // Assert
        client.Should().NotBeNull();
        host.Provider.GetRequiredService<PingenAccessTokens>().Should().BeSameAs(host.Provider.GetRequiredService<PingenAccessTokens>());
        host.Provider.GetRequiredService<PingenAuthenticationHandler>().Should().NotBeNull();
        host.Provider.GetServices<IValidateOptions<PingenOptions>>().Should().ContainItemsAssignableTo<PingenOptionsValidator>();
    }

    [Theory]
    [InlineData(PingenEnvironment.Production, "https://api.pingen.com/organisations")]
    [InlineData(PingenEnvironment.Staging, "https://api-staging.pingen.com/organisations")]
    public async Task When_no_base_address_is_configured_the_client_talks_to_the_environment_default(PingenEnvironment environment, string expected)
    {
        // Arrange
        using var host = new PingenTestHost(options => options.Environment = environment);
        host.Api.EnqueueOk("""{"data":[]}""");

        // Act
        await host.Client.GetAsync<JsonElement>("organisations", TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Url.Should().Be(new Uri(expected));
        host.Api.Request.Header("Accept").Should().Be(PingenClient.JsonApiMediaType);
    }

    [Fact]
    public async Task When_a_base_address_is_configured_it_overrides_the_environment_default()
    {
        // Arrange
        using var host = new PingenTestHost(options => options.BaseAddress = new("https://api.example.com/"));
        host.Api.EnqueueOk("""{"data":[]}""");

        // Act
        await host.Client.GetAsync<JsonElement>("organisations", TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Url.Should().Be(new Uri("https://api.example.com/organisations"));
    }

    private static IServiceCollection Configured(Dictionary<string, string?> values)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        services.AddPingen();

        return services;
    }
}
