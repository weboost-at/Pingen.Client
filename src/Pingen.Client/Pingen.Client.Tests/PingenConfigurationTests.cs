using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pingen.Client.Authentication;
using Pingen.Client.Batches;
using Pingen.Client.Deliveries.Ebills;
using Pingen.Client.Deliveries.Emails;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Files;
using Pingen.Client.Options;
using Pingen.Client.Organisations;
using Pingen.Client.Users;
using Pingen.Client.Webhooks;

namespace Pingen.Client.Tests;

public class PingenConfigurationTests : TestBase
{
    [Fact]
    public void When_a_Pingen_section_is_present_AddPingen_binds_every_option_from_it()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Pingen:ClientId"] = Data.Options.ClientId,
                ["Pingen:ClientSecret"] = Data.Options.ClientSecret,
                ["Pingen:Environment"] = "Staging",
                ["Pingen:Scopes"] = Data.Options.Scopes,
                ["Pingen:BaseAddress"] = Data.Options.BaseAddress.ToString(),
                ["Pingen:IdentityAddress"] = Data.Options.IdentityAddress.ToString()
            })
            .Build();

        var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddPingen();

        // Act
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<PingenOptions>>().Value;

        // Assert
        options.Scopes.Should().Be(Data.Options.Scopes);
        options.ClientId.Should().Be(Data.Options.ClientId);
        options.BaseAddress.Should().Be(Data.Options.BaseAddress);
        options.Environment.Should().Be(PingenEnvironment.Staging);
        options.ClientSecret.Should().Be(Data.Options.ClientSecret);
        options.IdentityAddress.Should().Be(Data.Options.IdentityAddress);
    }

    [Fact]
    public void When_the_section_omits_the_client_id_AddPingen_fails_the_startup_validation()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Pingen:ClientSecret"] = Data.Options.ClientSecret })
            .Build();

        var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).AddPingen();

        // Act
        using var provider = services.BuildServiceProvider();
        var act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        act.Should().Throw<OptionsValidationException>().WithMessage("*ClientId is required.*");
    }

    [Theory]
    [InlineData(typeof(PingenClient))]
    [InlineData(typeof(PingenAccessTokens))]
    [InlineData(typeof(PingenAuthenticationHandler))]
    [InlineData(typeof(LetterService))]
    [InlineData(typeof(EmailService))]
    [InlineData(typeof(EbillService))]
    [InlineData(typeof(BatchService))]
    [InlineData(typeof(OrganisationService))]
    [InlineData(typeof(UserService))]
    [InlineData(typeof(WebhookService))]
    [InlineData(typeof(FileService))]
    public void When_the_container_is_built_AddPingen_has_registered_the_service(Type service) =>
        Host.Provider.GetRequiredService(service).Should().BeOfType(service);

    [Fact]
    public void When_the_token_cache_is_resolved_twice_AddPingen_hands_out_the_same_one() =>
        Resolve<PingenAccessTokens>().Should().BeSameAs(Resolve<PingenAccessTokens>());

    [Theory]
    [InlineData(PingenEnvironment.Production, "https://api.pingen.com/organisations")]
    [InlineData(PingenEnvironment.Staging, "https://api-staging.pingen.com/organisations")]
    public async Task When_an_environment_is_set_AddPingen_targets_its_api(PingenEnvironment environment, string url)
    {
        // Arrange
        HostWith(options => options.Environment = environment);
        Host.Api.EnqueueOk("""{ "data": [] }""");

        // Act
        await Host.Client.GetAsync<JsonElement>("organisations", Ct);

        // Assert
        Host.Api.Request.Url.Should().Be(new Uri(url));
        Host.Api.Request.Header("Accept").Should().Be(PingenClient.JsonApiMediaType);
    }

    [Fact]
    public async Task When_a_base_address_is_set_AddPingen_targets_it_instead_of_the_environment()
    {
        // Arrange
        HostWith(options => options.BaseAddress = Data.Options.BaseAddress);
        Host.Api.EnqueueOk("""{ "data": [] }""");

        // Act
        await Host.Client.GetAsync<JsonElement>("organisations", Ct);

        // Assert
        Host.Api.Request.Url.Should().Be(new Uri(Data.Options.BaseAddress, "organisations"));
    }
}
