using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Tests.Common;

public class PingenErrorTests
{
    [Fact]
    public void When_the_api_rejects_a_request_Deserialize_maps_every_error_member()
    {
        // Arrange
        var json = """
                   {"errors":[
                     {"code":"1005","title":"The given data was invalid.","detail":"The file url signature is invalid.","source":{"pointer":"/data/attributes/file_url_signature"}},
                     {"title":"Unprocessable Entity","source":{"parameter":"page[limit]"}}
                   ]}
                   """;

        // Act
        var document = JsonSerializer.Deserialize<PingenErrorDocument>(json, PingenJson.Options)!;

        // Assert
        document.Errors.Should().HaveCount(2);
        document.Errors[0].Code.Should().Be("1005");
        document.Errors[0].Title.Should().Be("The given data was invalid.");
        document.Errors[0].Detail.Should().Be("The file url signature is invalid.");
        document.Errors[0].Source!.Pointer.Should().Be("/data/attributes/file_url_signature");
        document.Errors[0].Source!.Parameter.Should().BeNull();
        document.Errors[1].Code.Should().BeNull();
        document.Errors[1].Source!.Parameter.Should().Be("page[limit]");
    }
}
