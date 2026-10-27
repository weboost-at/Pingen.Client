using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common;

public class PingenListOptionsExtensionsTests : UnitTestBase
{
    [Fact]
    public void When_no_options_are_given_WithoutSort_returns_null() =>
        ((PingenListOptions?)null).WithoutSort().Should().BeNull();

    [Fact]
    public void When_options_are_given_WithoutSort_drops_the_sort_and_keeps_the_rest()
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
