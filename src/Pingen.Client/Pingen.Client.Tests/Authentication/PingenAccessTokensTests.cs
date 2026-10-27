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
    public async Task When_a_token_is_requested_GetAsync_posts_the_client_credentials_form(string scopes, string wire)
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
    public async Task When_no_scopes_are_configured_GetAsync_omits_the_scope_field(string? scopes)
    {
        // Arrange
        HostWith(options => options.Scopes = scopes);

        // Act
        await Tokens.GetAsync(Ct);

        // Assert
        Host.Identity.Request.Text.Should().NotContain("scope");
    }

    [Fact]
    public async Task When_the_staging_environment_is_configured_GetAsync_talks_to_the_staging_identity_host()
    {
        // Arrange
        HostWith(options => options.Environment = PingenEnvironment.Staging);

        // Act
        await Tokens.GetAsync(Ct);

        // Assert
        Host.Identity.Request.Url.Host.Should().Be("identity-staging.pingen.com");
    }

    [Fact]
    public async Task When_an_identity_address_is_configured_GetAsync_posts_to_it_instead()
    {
        // Arrange
        HostWith(options => options.IdentityAddress = Data.Options.IdentityAddress);

        // Act
        await Tokens.GetAsync(Ct);

        // Assert
        Host.Identity.Request.Url.Should().Be(new Uri(Data.Options.IdentityAddress, "auth/access-tokens"));
    }

    [Fact]
    public async Task When_a_cached_token_is_still_valid_GetAsync_does_not_request_a_second_one()
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
    public async Task When_the_cached_token_expires_within_the_refresh_window_GetAsync_requests_a_new_one(int expiresIn)
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

    [Fact]
    public async Task When_Invalidate_runs_during_a_refresh_GetAsync_does_not_cache_the_token_it_was_waiting_for()
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
    public async Task When_the_identity_host_rejects_the_credentials_GetAsync_throws_a_PingenException()
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
