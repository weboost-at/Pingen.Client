using System.Net;
using FluentAssertions;
using Pingen.Client.Options;
using Pingen.Client.Tests.Authentication.Support;

namespace Pingen.Client.Tests.Authentication;

public class PingenAccessTokensTests : AuthenticationTestBase
{
    [Theory]
    [InlineData("letter", "letter")]
    [InlineData("letter webhook user", "letter+webhook+user")]
    [InlineData("batch organisation_read", "batch+organisation_read")]
    public async Task GetAsync_Posts_The_Client_Credentials_Form(string scopes, string wire)
    {
        // Arrange
        HostWith(options => options.Scopes = scopes);

        // Act
        var token = await Tokens.GetAsync(Ct);

        // Assert
        token.Should().Be(PingenTestHost.AccessToken);

        Host.Identity.Request.Method.Should().Be(HttpMethod.Post);
        Host.Identity.Request.Header("Accept").Should().Be("application/json");
        Host.Identity.Request.Url.Should().Be(new Uri("https://identity.pingen.com/auth/access-tokens"));
        Host.Identity.Request.Header("Content-Type").Should().Be("application/x-www-form-urlencoded");
        Host.Identity.Request.Text.Should().Be(
            $"grant_type=client_credentials&client_id={PingenTestHost.ClientId}&client_secret={PingenTestHost.ClientSecret}&scope={wire}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Given_No_Scopes_GetAsync_Omits_The_Scope_Field(string? scopes)
    {
        // Arrange
        HostWith(options => options.Scopes = scopes);

        // Act
        await Tokens.GetAsync(Ct);

        // Assert
        Host.Identity.Request.Text.Should().NotContain("scope");
    }

    [Fact]
    public async Task Given_The_Staging_Environment_GetAsync_Talks_To_The_Staging_Identity_Host()
    {
        // Arrange
        HostWith(options => options.Environment = PingenEnvironment.Staging);

        // Act
        await Tokens.GetAsync(Ct);

        // Assert
        Host.Identity.Request.Url.Host.Should().Be("identity-staging.pingen.com");
    }

    [Fact]
    public async Task Given_An_Identity_Address_GetAsync_Posts_To_It_Instead()
    {
        // Arrange
        HostWith(options => options.IdentityAddress = Data.Options.IdentityAddress);

        // Act
        await Tokens.GetAsync(Ct);

        // Assert
        Host.Identity.Request.Url.Should().Be(new Uri(Data.Options.IdentityAddress, "auth/access-tokens"));
    }

    [Fact]
    public async Task Given_A_Valid_Cached_Token_GetAsync_Requests_No_Second_One()
    {
        // Act
        var first = await Tokens.GetAsync(Ct);
        var second = await Tokens.GetAsync(Ct);

        // Assert
        second.Should().Be(first);
        Host.Identity.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(59)]
    [InlineData(60)]
    public async Task Given_A_Token_Inside_The_Refresh_Window_GetAsync_Requests_A_New_One(int expiresIn)
    {
        // Arrange
        Host.Identity.Clear();
        Host.EnqueueToken(Authentication.ExpiringToken, expiresIn).EnqueueToken(Authentication.FreshToken);

        // Act
        var first = await Tokens.GetAsync(Ct);
        var second = await Tokens.GetAsync(Ct);

        // Assert
        first.Should().Be(Authentication.ExpiringToken);
        second.Should().Be(Authentication.FreshToken);
        Host.Identity.Requests.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(59_999, 1)]
    [InlineData(60_000, 2)]
    [InlineData(60_001, 2)]
    public async Task Given_A_Two_Minute_Token_GetAsync_Reuses_It_Until_A_Minute_Is_Left(int elapsed, int issued)
    {
        // Arrange
        Host.Identity.Clear();
        Host.EnqueueToken(Authentication.ExpiringToken, expiresIn: 120).EnqueueToken(Authentication.FreshToken);

        await Tokens.GetAsync(Ct);

        // Act
        Host.Clock.Advance(TimeSpan.FromMilliseconds(elapsed));
        await Tokens.GetAsync(Ct);

        // Assert
        Host.Identity.Requests.Should().HaveCount(issued);
    }

    [Fact]
    public async Task Given_An_Invalidate_During_A_Refresh_GetAsync_Does_Not_Cache_The_Token_It_Waited_For()
    {
        // Arrange
        Host.EnqueueToken(Authentication.FreshToken);

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Host.Identity.OnRequest = async _ =>
        {
            if (Host.Identity.Requests.Count is not 1) return;

            started.SetResult();
            await release.Task;
        };

        // Act
        var inflight = Tokens.GetAsync(Ct);
        await started.Task;

        Tokens.Invalidate();
        release.SetResult();

        var stale = await inflight;
        var refreshed = await Tokens.GetAsync(Ct);

        // Assert
        stale.Should().Be(PingenTestHost.AccessToken);
        refreshed.Should().Be(Authentication.FreshToken);
        Host.Identity.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_Rejected_Credentials_GetAsync_Throws_A_PingenException()
    {
        // Arrange
        Host.Identity.Clear().Enqueue(Rejection(HttpStatusCode.Unauthorized));

        // Act
        var act = () => Tokens.GetAsync(Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.Unauthorized);
        exception.RequestId.Should().Be(Request.Id);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Title.Should().Be(Error.Title);
    }
}
