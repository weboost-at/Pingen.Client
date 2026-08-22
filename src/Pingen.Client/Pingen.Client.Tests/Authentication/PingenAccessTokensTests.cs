using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pingen.Client.Authentication;
using Pingen.Client.Common;
using Pingen.Client.Options;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests.Authentication;

public class PingenAccessTokensTests
{
    [Fact]
    public async Task When_a_token_is_requested_GetAsync_posts_the_client_credentials_form_to_the_identity_host()
    {
        // Arrange
        using var host = new PingenTestHost(options => options.Scopes = "letter webhook user");

        // Act
        var token = await Tokens(host).GetAsync(TestContext.Current.CancellationToken);

        // Assert
        token.Should().Be(PingenTestHost.AccessToken);
        host.Identity.Request.Method.Should().Be(HttpMethod.Post);
        host.Identity.Request.Url.Should().Be(new Uri("https://identity.pingen.com/auth/access-tokens"));
        host.Identity.Request.Header("Content-Type").Should().Be("application/x-www-form-urlencoded");
        host.Identity.Request.Text.Should().Be("grant_type=client_credentials&client_id=test-client-id&client_secret=test-client-secret&scope=letter+webhook+user");
    }

    [Fact]
    public async Task When_no_scopes_are_configured_GetAsync_omits_the_scope_field()
    {
        // Arrange
        using var host = new PingenTestHost();

        // Act
        await Tokens(host).GetAsync(TestContext.Current.CancellationToken);

        // Assert
        host.Identity.Request.Text.Should().NotContain("scope");
    }

    [Fact]
    public async Task When_the_staging_environment_is_configured_GetAsync_talks_to_the_staging_identity_host()
    {
        // Arrange
        using var host = new PingenTestHost(options => options.Environment = PingenEnvironment.Staging);

        // Act
        await Tokens(host).GetAsync(TestContext.Current.CancellationToken);

        // Assert
        host.Identity.Request.Url.Host.Should().Be("identity-staging.pingen.com");
    }

    [Fact]
    public async Task When_a_cached_token_is_still_valid_GetAsync_does_not_request_a_second_one()
    {
        // Arrange
        using var host = new PingenTestHost();
        var tokens = Tokens(host);

        // Act
        var first = await tokens.GetAsync(TestContext.Current.CancellationToken);
        var second = await tokens.GetAsync(TestContext.Current.CancellationToken);

        // Assert
        second.Should().Be(first);
        host.Identity.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(59)]
    public async Task When_the_cached_token_expires_within_the_refresh_window_GetAsync_requests_a_new_one(int expiresIn)
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Identity.Clear();
        host.EnqueueToken("expiring", expiresIn);
        host.EnqueueToken("fresh");
        var tokens = Tokens(host);

        // Act
        var first = await tokens.GetAsync(TestContext.Current.CancellationToken);
        var second = await tokens.GetAsync(TestContext.Current.CancellationToken);

        // Assert
        first.Should().Be("expiring");
        second.Should().Be("fresh");
        host.Identity.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task When_Invalidate_runs_while_a_refresh_is_in_flight_GetAsync_does_not_cache_the_token_it_was_waiting_for()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Identity.Clear();
        host.EnqueueToken("rejected").EnqueueToken("fresh");
        var tokens = Tokens(host);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Identity.OnRequest = async _ =>
        {
            if (host.Identity.Requests.Count is not 1) return;

            started.SetResult();
            await release.Task;
        };

        // Act
        var inflight = tokens.GetAsync(TestContext.Current.CancellationToken);
        await started.Task;
        tokens.Invalidate();
        release.SetResult();
        var stale = await inflight;
        var refreshed = await tokens.GetAsync(TestContext.Current.CancellationToken);

        // Assert
        stale.Should().Be("rejected");
        refreshed.Should().Be("fresh");
        host.Identity.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task When_the_identity_host_rejects_the_credentials_GetAsync_throws_a_PingenException()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Identity.Clear().EnqueueJson(
            HttpStatusCode.Unauthorized,
            """{"errors":[{"code":"1002","title":"Unauthorized"}]}""",
            "application/json"
        );

        // Act
        var act = () => Tokens(host).GetAsync(TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.Errors.Should().ContainSingle().Which.Title.Should().Be("Unauthorized");
    }

    private static PingenAccessTokens Tokens(PingenTestHost host) => host.Provider.GetRequiredService<PingenAccessTokens>();
}
