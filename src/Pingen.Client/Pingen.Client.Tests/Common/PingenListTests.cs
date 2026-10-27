using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Tests.Common;

public class PingenListTests : UnitTestBase
{
    [Fact]
    public void A_Wrapped_Page_Is_Enumerated_With_Its_Links_And_Meta()
    {
        // Arrange
        var links = new ListLinks { Next = "https://api.pingen.com/letters?page[number]=2" };
        var meta = new ListMeta { CurrentPage = 1, LastPage = 2, PerPage = 2, From = 1, To = 2, Total = 3 };

        // Act
        var page = new PingenList<string>(
            Data: ["Zürich", "Genève"],
            Links: links,
            Meta: meta
        );

        // Assert
        page.Count.Should().Be(2);
        page[1].Should().Be("Genève");
        page.Should().Equal("Zürich", "Genève");

        page.Meta.Should().BeSameAs(meta);
        page.Links.Should().BeSameAs(links);
    }
}
