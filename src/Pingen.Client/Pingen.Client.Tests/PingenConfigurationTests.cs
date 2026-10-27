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
    public void Given_A_Pingen_Section_AddPingen_Binds_Every_Option_From_It()
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
    public void Given_No_Client_Id_AddPingen_Fails_The_Startup_Validation()
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
    public void AddPingen_Registers_The_Service(Type service) =>
        Host.Provider.GetRequiredService(service).Should().BeOfType(service);

    [Fact]
    public void AddPingen_Authenticates_The_Client_And_Leaves_Redirects_Unfollowed()
    {
        // Arrange
        using var provider = new ServiceCollection().AddPingen(_ => { }).BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpMessageHandlerFactory>();

        // Act
        List<HttpMessageHandler> chain = [factory.CreateHandler(nameof(PingenClient))];
        while (chain[^1] is DelegatingHandler { InnerHandler: { } inner }) chain.Add(inner);

        // Assert
        chain.OfType<PingenAuthenticationHandler>().Should().ContainSingle();
        chain.OfType<HttpClientHandler>().Select(handler => handler.AllowAutoRedirect).Should().Equal(false);
    }

    [Fact]
    public void AddPingen_Leaves_The_Files_Client_Unauthenticated()
    {
        // Arrange
        using var provider = new ServiceCollection().AddPingen(_ => { }).BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpMessageHandlerFactory>();

        // Act
        List<HttpMessageHandler> chain = [factory.CreateHandler(PingenClient.FilesClientName)];
        while (chain[^1] is DelegatingHandler { InnerHandler: { } inner }) chain.Add(inner);

        // Assert
        chain.OfType<PingenAuthenticationHandler>().Should().BeEmpty();
    }

    [Fact]
    public void Given_No_Clock_AddPingen_Falls_Back_To_The_System_Clock()
    {
        // Arrange
        using var provider = new ServiceCollection().AddPingen(_ => { }).BuildServiceProvider();

        // Act
        var clock = provider.GetRequiredService<TimeProvider>();

        // Assert
        clock.Should().BeSameAs(TimeProvider.System);
        provider.GetRequiredService<PingenAccessTokens>().Should().NotBeNull();
    }

    [Fact]
    public void AddPingen_Hands_Out_One_Token_Cache() =>
        Resolve<PingenAccessTokens>().Should().BeSameAs(Resolve<PingenAccessTokens>());

    [Theory]
    [InlineData(PingenEnvironment.Production, "https://api.pingen.com/organisations")]
    [InlineData(PingenEnvironment.Staging, "https://api-staging.pingen.com/organisations")]
    public async Task Given_An_Environment_AddPingen_Targets_Its_Api(PingenEnvironment environment, string url)
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
    public async Task Given_A_Base_Address_AddPingen_Targets_It_Instead_Of_The_Environment()
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
