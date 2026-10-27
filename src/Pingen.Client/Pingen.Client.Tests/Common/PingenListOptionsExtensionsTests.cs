using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common;

public class PingenListOptionsExtensionsTests : UnitTestBase
{
    [Fact]
    public void Given_No_Options_WithoutSort_Returns_Null() =>
        ((PingenListOptions?)null).WithoutSort().Should().BeNull();

    [Fact]
    public void Given_Options_WithoutSort_Drops_The_Sort_And_Keeps_The_Rest()
    {
        // Arrange
        var options = new PingenListOptions
        {
            PageNumber = 2,
            PageLimit = 50,
            Sort = "-created_at",
            Filter = PingenFilter.Where("status", "sent"),
            Search = "Zürich AG",
            Include = "organisation",
            Language = "de-DE",
            Fields = new Dictionary<string, string> { ["letters"] = "status" }
        };

        // Act
        var withoutSort = options.WithoutSort();

        // Assert
        withoutSort.Should().Be(options with { Sort = null });
    }
}
