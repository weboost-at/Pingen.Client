using System.Net;
using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common;

public class PingenExceptionTests : UnitTestBase
{
    [Fact]
    public void When_the_api_reports_errors_PingenException_carries_them_with_the_status_and_headers()
    {
        // Arrange
        PingenError[] errors = [new() { Code = Data.Error.Code, Title = Data.Error.Title }];

        // Act
        var exception = new PingenException(
            statusCode: HttpStatusCode.TooManyRequests,
            errors: errors,
            requestId: Data.Request.Id,
            retryAfter: Data.Request.RetryAfter
        );

        // Assert
        exception.Errors.Should().BeSameAs(errors);
        exception.RequestId.Should().Be(Data.Request.Id);
        exception.RetryAfter.Should().Be(Data.Request.RetryAfter);
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        exception.Message.Should().Be($"Pingen request failed with 429 TooManyRequests: {Data.Error.Title}");
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
