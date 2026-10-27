using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Tests.Common;

public class PingenErrorTests : UnitTestBase
{
    [Fact]
    public void Deserialize_Maps_Every_Error_Member()
    {
        // Act
        var document = JsonSerializer.Deserialize<PingenErrorDocument>(
            $$"""
              {
                "errors": [
                  {
                    "code": "{{Data.Error.Code}}",
                    "title": "{{Data.Error.Title}}",
                    "detail": "{{Data.Error.Detail}}",
                    "source": { "pointer": "{{Data.Error.Pointer}}" }
                  },
                  { "title": "Unprocessable Entity", "source": { "parameter": "page[limit]" } }
                ]
              }
              """,
            PingenJson.Options
        )!;

        // Assert
        document.Errors.Should().HaveCount(2);

        document.Errors[0].Code.Should().Be(Data.Error.Code);
        document.Errors[0].Title.Should().Be(Data.Error.Title);
        document.Errors[0].Detail.Should().Be(Data.Error.Detail);
        document.Errors[0].Source!.Pointer.Should().Be(Data.Error.Pointer);
        document.Errors[0].Source!.Parameter.Should().BeNull();

        document.Errors[1].Code.Should().BeNull();
        document.Errors[1].Source!.Parameter.Should().Be("page[limit]");
    }
}
