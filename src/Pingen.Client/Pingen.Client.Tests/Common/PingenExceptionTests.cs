using System.Net;
using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common;

public class PingenExceptionTests : UnitTestBase
{
    [Fact]
    public void Given_Errors_It_Carries_Them_With_The_Status_And_Headers()
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
    public void Given_No_Title_The_Message_Falls_Back_To_Detail_Or_Code(string? detail, string code, string shown)
    {
        // Act
        var exception = new PingenException(HttpStatusCode.UnprocessableEntity, [new() { Code = code, Detail = detail }]);

        // Assert
        exception.Message.Should().Be($"Pingen request failed with 422 UnprocessableEntity: {shown}");
    }

    [Fact]
    public void Given_No_Errors_The_Message_Still_Names_The_Status()
    {
        // Act
        var exception = new PingenException(HttpStatusCode.ServiceUnavailable, []);

        // Assert
        exception.Errors.Should().BeEmpty();
        exception.RetryAfter.Should().BeNull();
        exception.Message.Should().Be("Pingen request failed with 503 ServiceUnavailable");
    }
}
