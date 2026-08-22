using System.Net;
using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common;

public class PingenExceptionTests
{
    [Fact]
    public void When_the_api_reports_errors_PingenException_carries_them_with_the_status_and_headers()
    {
        // Arrange
        var errors = new[] { new PingenError { Code = "1005", Title = "The given data was invalid." } };

        // Act
        var exception = new PingenException(
            statusCode: HttpStatusCode.TooManyRequests,
            errors: errors,
            requestId: "0HN7A2K3L4M5N",
            retryAfter: TimeSpan.FromSeconds(30)
        );

        // Assert
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        exception.Errors.Should().ContainSingle().Which.Code.Should().Be("1005");
        exception.RequestId.Should().Be("0HN7A2K3L4M5N");
        exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
        exception.Message.Should().Be("Pingen request failed with 429 TooManyRequests: The given data was invalid.");
    }

    [Fact]
    public void When_the_body_carries_no_errors_PingenException_still_names_the_status()
    {
        // Act
        var exception = new PingenException(HttpStatusCode.ServiceUnavailable, []);

        // Assert
        exception.Errors.Should().BeEmpty();
        exception.RetryAfter.Should().BeNull();
        exception.Message.Should().Be("Pingen request failed with 503 ServiceUnavailable");
    }
}
