using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests;

public class PingenClientTests
{
    [Fact]
    public async Task When_list_options_are_given_GetAsync_appends_the_composed_query()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk("""{"data":[]}""");
        var options = new PingenListOptions
        {
            PageNumber = 2,
            PageLimit = 100,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "sent"),
        };

        // Act
        await host.Client.GetAsync<JsonElement>("organisations/1/letters", options, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Path.Should().Be("/organisations/1/letters");
        host.Api.Request.Query.Should().Be("?page[number]=2&page[limit]=100&sort=-created_at&filter=%7B%22status%22%3A%22sent%22%7D");
    }

    [Fact]
    public async Task When_a_body_and_an_idempotency_key_are_given_SendAsync_writes_a_json_api_request()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueJson(HttpStatusCode.Created, """{"data":{"id":"42","type":"letters"}}""");

        // Act
        var document = await host.Client.SendAsync<JsonElement>(
            method: HttpMethod.Post,
            path: "organisations/1/letters",
            body: new { data = new { type = "letters", attributes = new { file_url = "https://files.example.com/1" } } },
            requestOptions: new() { IdempotencyKey = "3f1c6c9a-0e2f-4b1e-9f5d-1c2b3a4d5e6f" },
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Post);
        host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        host.Api.Request.Header("Idempotency-Key").Should().Be("3f1c6c9a-0e2f-4b1e-9f5d-1c2b3a4d5e6f");
        host.Api.Request.Text.Should().Be("""{"data":{"type":"letters","attributes":{"file_url":"https://files.example.com/1"}}}""");
        document.GetProperty("data").GetProperty("id").GetString().Should().Be("42");
    }

    [Fact]
    public async Task When_the_endpoint_answers_without_a_payload_SendAsync_returns_after_the_call()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await host.Client.SendAsync(HttpMethod.Patch, "organisations/1/letters/2/cancel", null, null, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Patch);
        host.Api.Request.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task When_the_api_answers_an_error_document_the_request_throws_a_PingenException_carrying_it()
    {
        // Arrange
        using var host = new PingenTestHost();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(
                """{"errors":[{"code":"1005","title":"Too Many Attempts.","detail":"Slow down","source":{"parameter":"page[limit]"}}]}""",
                Encoding.UTF8,
                PingenClient.JsonApiMediaType
            ),
        };
        response.Headers.Add("X-Request-Id", "0HN7A2K3L4M5N");
        response.Headers.Add("Retry-After", "30");
        host.Api.Enqueue(response);

        // Act
        var act = () => host.Client.GetAsync<JsonElement>("organisations", TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        exception.RequestId.Should().Be("0HN7A2K3L4M5N");
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
        exception.Errors.Should().ContainSingle().Which.Source!.Parameter.Should().Be("page[limit]");
    }

    [Fact]
    public async Task When_the_error_body_is_not_json_the_request_still_throws_with_the_status()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.Enqueue(new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("<html>maintenance</html>", Encoding.UTF8, "text/html") });

        // Act
        var act = () => host.Client.GetAsync<JsonElement>("organisations", TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_file_endpoint_answers_302_GetLocationAsync_returns_the_presigned_url_unfollowed()
    {
        // Arrange
        using var host = new PingenTestHost();
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new("https://files.pingen.com/letters/1.pdf?signature=abc");
        host.Api.Enqueue(response);

        // Act
        var location = await host.Client.GetLocationAsync("organisations/1/letters/2/file", TestContext.Current.CancellationToken);

        // Assert
        location.Should().Be(new Uri("https://files.pingen.com/letters/1.pdf?signature=abc"));
        host.Api.Requests.Should().ContainSingle();
        host.Files.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_presigned_url_is_fetched_the_file_client_sends_no_authorization_header()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Files.Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });

        // Act
        await host.Client.FileClient.GetAsync("https://files.pingen.com/letters/1.pdf?signature=abc", TestContext.Current.CancellationToken);

        // Assert
        host.Files.Request.Header("Authorization").Should().BeNull();
        host.Identity.Requests.Should().BeEmpty();
    }
}
