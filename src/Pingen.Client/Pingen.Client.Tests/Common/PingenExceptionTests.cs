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
        PingenError[] errors = [new() { Code = Data.Error.Code, Title = Data.Error.Title, Detail = Data.Error.Detail }];

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

    [Theory]
    [InlineData("Der Brief ist noch nicht gültig", "1005", "Der Brief ist noch nicht gültig")]
    [InlineData(null, "1005", "1005")]
    public void When_the_title_is_null_Message_falls_back_to_detail_or_code(string? detail, string code, string shown)
    {
        // Act
        var exception = new PingenException(HttpStatusCode.UnprocessableEntity, [new() { Code = code, Detail = detail }]);

        // Assert
        exception.Message.Should().Be($"Pingen request failed with 422 UnprocessableEntity: {shown}");
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
