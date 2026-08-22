using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Tests.Common;

public class PingenListTests
{
    [Fact]
    public void When_a_page_is_wrapped_PingenList_enumerates_it_and_keeps_links_and_meta()
    {
        // Arrange
        var document = new ListDocument<string>
        {
            Data = ["first", "second"],
            Links = new() { Next = "https://api.pingen.com/letters?page[number]=2" },
            Meta = new() { CurrentPage = 1, LastPage = 2, PerPage = 2, From = 1, To = 2, Total = 3 },
        };

        // Act
        var page = new PingenList<string>(document.Data, document.Links, document.Meta);

        // Assert
        page.Count.Should().Be(2);
        page[1].Should().Be("second");
        page.Should().Equal("first", "second");
        page.Links!.Next.Should().Be("https://api.pingen.com/letters?page[number]=2");
        page.Meta!.Total.Should().Be(3);
    }
}
