using System.Net;
using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Authentication;
using Pingen.Client.Tests.Authentication.Support;

namespace Pingen.Client.Tests.Authentication;

public class PingenAuthenticationHandlerTests : AuthenticationTestBase
{
    [Fact]
    public async Task When_two_requests_are_sent_SendAsync_authenticates_both_from_one_token_request()
    {
        // Arrange
        Host.Api.EnqueueOk("""{ "data": [] }""").EnqueueOk("""{ "data": [] }""");

        // Act
        await Host.Client.GetAsync<JsonElement>("organisations", Ct);
        await Host.Client.GetAsync<JsonElement>("organisations", Ct);

        // Assert
        Host.Identity.Requests.Should().ContainSingle();

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests
            .Select(request => request.Header("Authorization"))
            .Should().AllBe($"Bearer {PingenTestHost.AccessToken}");
    }

    [Fact]
    public async Task When_the_api_rejects_the_token_SendAsync_replays_the_request_once_with_a_fresh_token()
    {
        // Arrange
        Host.EnqueueToken(Authentication.FreshToken);
        Host.Api.Enqueue(Rejection(HttpStatusCode.Unauthorized)).EnqueueOk("""{ "data": { "type": "letters" } }""");

        // Act
        await Host.Client.SendAsync<JsonElement>(
            method: HttpMethod.Post,
            path: $"organisations/{Organisation.Id}/letters",
            body: new { data = new { type = "letters" } },
            requestOptions: new() { IdempotencyKey = Request.IdempotencyKey },
            cancellationToken: Ct
        );

        // Assert
        Host.Identity.Requests.Should().HaveCount(2);

        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[1].Text.Should().Be(Host.Api.Requests[0].Text);
        Host.Api.Requests[1].Header("Accept").Should().Be(PingenClient.JsonApiMediaType);
        Host.Api.Requests[1].Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);
        Host.Api.Requests[1].Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        Host.Api.Requests[1].Header("Authorization").Should().Be($"Bearer {Authentication.FreshToken}");
        Host.Api.Requests[0].Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");
    }

    [Fact]
    public async Task When_an_http2_request_is_rejected_SendAsync_replays_it_over_http2()
    {
        // Arrange
        Host.EnqueueToken(Authentication.FreshToken);
        Host.Api.Enqueue(Rejection(HttpStatusCode.Unauthorized)).EnqueueOk("""{ "data": [] }""");

        var handler = Resolve<PingenAuthenticationHandler>();
        handler.InnerHandler = Host.Api;

        using var invoker = new HttpMessageInvoker(handler);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.pingen.com/organisations")
        {
            Version = HttpVersion.Version20
        };

        // Act
        using var response = await invoker.SendAsync(request, Ct);

        // Assert
        Host.Api.Requests.Should().HaveCount(2);
        Host.Api.Requests[1].Version.Should().Be(HttpVersion.Version20);
    }

    [Fact]
    public async Task When_the_api_rejects_the_token_SendAsync_disposes_the_rejected_response()
    {
        // Arrange
        var rejection = Rejection(HttpStatusCode.Unauthorized);

        Host.EnqueueToken(Authentication.FreshToken);
        Host.Api.Enqueue(rejection).EnqueueOk("""{ "data": [] }""");

        // Act
        await Host.Client.GetAsync<JsonElement>("organisations", Ct);

        // Assert
        await rejection.Content
            .Awaiting(content => content.ReadAsStringAsync(Ct))
            .Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task When_the_replayed_request_is_rejected_too_SendAsync_throws_without_retrying_again()
    {
        // Arrange
        Host.EnqueueToken(Authentication.FreshToken);
        Host.Api.Enqueue(Rejection(HttpStatusCode.Unauthorized)).Enqueue(Rejection(HttpStatusCode.Unauthorized));

        // Act
        var act = () => Host.Client.GetAsync<JsonElement>("organisations", Ct);

        // Assert
        await act.Assert.ThrowsAsync(HttpStatusCode.Unauthorized);

        Host.Api.Requests.Should().HaveCount(2);
    }
}
