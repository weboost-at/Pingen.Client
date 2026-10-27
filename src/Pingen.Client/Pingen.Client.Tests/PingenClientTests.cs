using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests;

public class PingenClientTests : TestBase
{
    [Theory]
    [InlineData(nameof(PingenClient.Letters))]
    [InlineData(nameof(PingenClient.Emails))]
    [InlineData(nameof(PingenClient.Ebills))]
    [InlineData(nameof(PingenClient.Batches))]
    [InlineData(nameof(PingenClient.Organisations))]
    [InlineData(nameof(PingenClient.Users))]
    [InlineData(nameof(PingenClient.Webhooks))]
    [InlineData(nameof(PingenClient.Files))]
    public void When_a_hub_property_is_read_twice_it_returns_the_same_service(string service)
    {
        // Arrange
        var client = Host.Client;
        var property = typeof(PingenClient).GetProperty(service)!;

        // Act
        var first = property.GetValue(client);
        var second = property.GetValue(client);

        // Assert
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task When_list_options_are_given_GetAsync_appends_the_composed_query()
    {
        // Arrange
        Host.Api.EnqueueOk("""{ "data": [] }""");

        var options = new PingenListOptions
        {
            PageNumber = 2,
            PageLimit = 100,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "sent")
        };

        // Act
        await Host.Client.GetAsync<JsonElement>($"organisations/{Organisation.Id}/letters", options, Ct);

        // Assert
        Host.Api.Request.Assert.Sent(HttpMethod.Get, $"/organisations/{Organisation.Id}/letters");
        Host.Api.Request.Query.Should().Be("?page[number]=2&page[limit]=100&sort=-created_at&filter=%7B%22status%22%3A%22sent%22%7D");
    }

    [Fact]
    public async Task When_a_body_and_an_idempotency_key_are_given_SendAsync_writes_a_json_api_request()
    {
        // Arrange
        Host.Api.EnqueueJson(HttpStatusCode.Created, $$"""{ "data": { "id": "{{Data.Letter.Id}}", "type": "letters" } }""");

        // Act
        var document = await Host.Client.SendAsync<JsonElement>(
            method: HttpMethod.Post,
            path: $"organisations/{Organisation.Id}/letters",
            body: new { data = new { attributes = new { file_url = File.Url.ToString() }, type = "letters" } },
            requestOptions: new() { IdempotencyKey = Request.IdempotencyKey },
            cancellationToken: Ct
        );

        // Assert
        document.GetProperty("data").Assert.Has("id", Data.Letter.Id.ToString());

        Host.Api.Request.Method.Should().Be(HttpMethod.Post);
        Host.Api.Request.Header("Content-Type").Should().Be(PingenClient.JsonApiMediaType);
        Host.Api.Request.Header("Idempotency-Key").Should().Be(Request.IdempotencyKey);
        Host.Api.Request.Text.Should().Be($$$"""{"data":{"attributes":{"file_url":"{{{File.Url}}}"},"type":"letters"}}""");
    }

    [Fact]
    public async Task When_the_endpoint_answers_without_a_payload_SendAsync_returns_after_the_call()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        await Host.Client.SendAsync(
            HttpMethod.Patch,
            $"organisations/{Organisation.Id}/letters/{Data.Letter.Id}/cancel",
            body: null,
            requestOptions: null,
            Ct
        );

        // Assert
        Host.Api.Request.Assert.Bodyless();
        Host.Api.Request.Method.Should().Be(HttpMethod.Patch);
    }

    [Fact]
    public async Task When_a_payload_is_expected_but_the_body_is_empty_SendAsync_throws_instead_of_returning_null()
    {
        // Arrange
        Host.Api.EnqueueEmpty();

        // Act
        var act = () => Host.Client.SendAsync<JsonElement>(
            HttpMethod.Post,
            $"organisations/{Organisation.Id}/letters",
            body: null,
            requestOptions: null,
            Ct
        );

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.NoContent);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Title.Should().Be("The response carried no payload");
        exception.Errors[0].Detail.Should().Be(
            $"POST organisations/{Organisation.Id}/letters answered 204 with an empty body where a resource was expected.");
    }

    [Fact]
    public async Task When_the_endpoint_may_skip_the_payload_SendOrDefaultAsync_returns_null_for_an_empty_body()
    {
        // Arrange
        Host.Api.EnqueueEmpty(HttpStatusCode.Accepted);

        // Act
        var document = await Host.Client.SendOrDefaultAsync<string>(
            HttpMethod.Post,
            $"organisations/{Organisation.Id}/letters/price-calculator",
            body: null,
            requestOptions: null,
            Ct
        );

        // Assert
        document.Should().BeNull();

        Host.Api.Request.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task When_the_api_answers_an_error_document_GetAsync_throws_with_its_errors_and_request_id()
    {
        // Arrange
        var response = Rejection(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new(Request.RetryAfter);

        Host.Api.Enqueue(response);

        // Act
        var act = () => Host.Client.GetAsync<JsonElement>("organisations", Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.TooManyRequests);
        exception.RequestId.Should().Be(Request.Id);
        exception.RetryAfter.Should().Be(Request.RetryAfter);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Code.Should().Be(Error.Code);
        exception.Errors[0].Source!.Pointer.Should().Be(Error.Pointer);
    }

    [Theory]
    [InlineData("<html>maintenance</html>", "text/html")]
    [InlineData("", PingenClient.JsonApiMediaType)]
    [InlineData("[]", PingenClient.JsonApiMediaType)]
    [InlineData("{}", PingenClient.JsonApiMediaType)]
    [InlineData("null", PingenClient.JsonApiMediaType)]
    [InlineData("""{ "errors": null }""", PingenClient.JsonApiMediaType)]
    public async Task When_the_error_body_lists_no_errors_GetAsync_throws_with_none(string body, string mediaType)
    {
        // Arrange
        Host.Api.Enqueue(new(HttpStatusCode.ServiceUnavailable) { Content = new StringContent(body, Encoding.UTF8, mediaType) });

        // Act
        var act = () => Host.Client.GetAsync<JsonElement>("organisations", Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.ServiceUnavailable);
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_file_endpoint_answers_302_GetLocationAsync_returns_the_presigned_url_unfollowed()
    {
        // Arrange
        Host.Api.EnqueueRedirect(File.Location);

        // Act
        var location = await Host.Client.GetLocationAsync($"organisations/{Organisation.Id}/letters/{Data.Letter.Id}/file", Ct);

        // Assert
        location.Should().Be(File.Location);

        Host.Files.Requests.Should().BeEmpty();
        Host.Api.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.OK)]
    public async Task When_no_location_is_answered_GetLocationAsync_throws_with_the_answer(HttpStatusCode status)
    {
        // Arrange
        Host.Api.Enqueue(Rejection(status));

        // Act
        var act = () => Host.Client.GetLocationAsync($"organisations/{Organisation.Id}/letters/{Data.Letter.Id}/file", Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(status);
        exception.RequestId.Should().Be(Request.Id);
        exception.Errors.Should().ContainSingle();
        exception.Errors[0].Title.Should().Be(Error.Title);
    }

    [Fact]
    public async Task When_a_presigned_url_is_fetched_FileClient_sends_no_authorization_header()
    {
        // Arrange
        Host.Files.EnqueueBytes(File.Pdf);

        // Act
        using var response = await Host.Client.FileClient.GetAsync(File.Location, Ct);

        // Assert
        Host.Files.Request.Assert.Unauthenticated();
        Host.Identity.Requests.Should().BeEmpty();
    }
}
