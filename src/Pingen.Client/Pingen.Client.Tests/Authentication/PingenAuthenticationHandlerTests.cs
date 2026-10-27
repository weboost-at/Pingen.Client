using System.Net;
using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Authentication;
using Pingen.Client.Tests.Authentication.Support;

namespace Pingen.Client.Tests.Authentication;

public class PingenAuthenticationHandlerTests : AuthenticationTestBase
{
    [Fact]
    public async Task Given_Two_Requests_Both_Are_Authenticated_From_One_Token_Request()
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
    public async Task Given_A_Rejected_Token_The_Request_Is_Replayed_Once_With_A_Fresh_One()
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
    public async Task Given_A_Rejected_Http2_Request_The_Replay_Keeps_Http2()
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
    public async Task Given_A_Rejected_Token_The_Rejected_Response_Is_Disposed()
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
    public async Task Given_A_Rejected_Replay_It_Throws_Without_Retrying_Again()
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
