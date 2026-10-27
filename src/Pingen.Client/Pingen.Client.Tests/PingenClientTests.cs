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
    public void Given_A_Hub_Property_Read_Twice_It_Returns_The_Same_Service(string service)
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
    public async Task Given_List_Options_GetAsync_Appends_The_Composed_Query()
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
    public async Task Given_A_Body_And_An_Idempotency_Key_SendAsync_Writes_A_Json_Api_Request()
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
    public async Task Given_An_Answer_Without_A_Payload_SendAsync_Returns_After_The_Call()
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
    public async Task Given_An_Empty_Body_Where_A_Payload_Is_Due_SendAsync_Throws_Instead_Of_Returning_Null()
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
    public async Task Given_An_Empty_Body_SendOrDefaultAsync_Returns_Null()
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
    public async Task Given_An_Error_Document_GetAsync_Throws_With_Its_Errors_And_Request_Id()
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
    public async Task Given_An_Error_Body_Without_Errors_GetAsync_Throws_With_None(string body, string mediaType)
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
    public async Task Given_A_302_GetLocationAsync_Returns_The_Presigned_Url_Unfollowed()
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
    public async Task Given_No_Location_GetLocationAsync_Throws_With_The_Answer(HttpStatusCode status)
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
    public async Task FileClient_Sends_No_Authorization_Header()
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
