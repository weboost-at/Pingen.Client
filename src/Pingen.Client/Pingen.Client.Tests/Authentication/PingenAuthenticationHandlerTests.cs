using System.Net;
using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests.Authentication;

public class PingenAuthenticationHandlerTests
{
    [Fact]
    public async Task When_two_requests_are_sent_SendAsync_authenticates_both_from_one_token_request()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk("""{"data":[]}""").EnqueueOk("""{"data":[]}""");

        // Act
        await host.Client.GetAsync<JsonElement>("organisations", TestContext.Current.CancellationToken);
        await host.Client.GetAsync<JsonElement>("organisations", TestContext.Current.CancellationToken);

        // Assert
        host.Identity.Requests.Should().ContainSingle();
        host.Api.Requests.Should().HaveCount(2);
        host.Api.Requests.Should().AllSatisfy(request => request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}"));
    }

    [Fact]
    public async Task When_the_api_rejects_the_token_SendAsync_replays_the_request_once_with_a_fresh_token()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.EnqueueToken("refreshed-token");
        host.Api.EnqueueJson(HttpStatusCode.Unauthorized, """{"errors":[{"code":"1002","title":"Unauthenticated"}]}""").EnqueueOk("""{"data":{"id":"1"}}""");

        // Act
        await host.Client.SendAsync<JsonElement>(
            method: HttpMethod.Post,
            path: "organisations/1/letters",
            body: new { data = new { type = "letters" } },
            requestOptions: null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Identity.Requests.Should().HaveCount(2);
        host.Api.Requests.Should().HaveCount(2);
        host.Api.Requests[0].Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");
        host.Api.Requests[1].Header("Authorization").Should().Be("Bearer refreshed-token");
        host.Api.Requests[1].Text.Should().Be(host.Api.Requests[0].Text);
        host.Api.Requests[1].Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
    }

    [Fact]
    public async Task When_the_replayed_request_is_rejected_too_SendAsync_throws_without_retrying_again()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.EnqueueToken("refreshed-token");
        host.Api
            .EnqueueJson(HttpStatusCode.Unauthorized, """{"errors":[{"code":"1002","title":"Unauthenticated"}]}""")
            .EnqueueJson(HttpStatusCode.Unauthorized, """{"errors":[{"code":"1002","title":"Unauthenticated"}]}""");

        // Act
        var act = () => host.Client.GetAsync<JsonElement>("organisations", TestContext.Current.CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<PingenException>()).Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        host.Api.Requests.Should().HaveCount(2);
    }
}
