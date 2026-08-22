using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common;

public class PingenQueryTests
{
    [Fact]
    public void When_no_option_is_set_Build_returns_an_empty_string()
    {
        // Act
        var withoutOptions = PingenQuery.Build(null);
        var withEmptyOptions = PingenQuery.Build(new PingenListOptions());

        // Assert
        withoutOptions.Should().BeEmpty();
        withEmptyOptions.Should().BeEmpty();
    }

    [Fact]
    public void When_every_option_is_set_Build_emits_the_json_api_query_parameters()
    {
        // Arrange
        var options = new PingenListOptions
        {
            PageNumber = 2,
            PageLimit = 50,
            Sort = "-created_at,real_id",
            Filter = PingenFilter.And(
                PingenFilter.Where("status", "sent"),
                PingenFilter.GreaterThan("created_at", new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)))
            ),
            Search = "Zürich AG",
            Include = "organisation,batch",
            Language = "de-DE",
            Fields = new Dictionary<string, string>
            {
                ["organisations"] = "name",
                ["letters"] = "status,created_at",
            },
        };

        // Act
        var query = PingenQuery.Build(options);

        // Assert
        query.Should().Be(
            "?page[number]=2&page[limit]=50&sort=-created_at,real_id" +
            "&filter=%7B%22and%22%3A%5B%7B%22status%22%3A%22sent%22%7D%2C%7B%22created_at%22%3A%22%3E2021-11-19T09%3A42%3A48%2B01%3A00%22%7D%5D%7D" +
            "&q=Z%C3%BCrich%20AG&include=organisation,batch&language=de-DE" +
            "&fields[letters]=status,created_at&fields[organisations]=name");
    }

    [Fact]
    public void When_only_paging_is_set_Build_emits_nothing_else()
    {
        // Act
        var query = PingenQuery.Build(new PingenListOptions { PageLimit = 100 });

        // Assert
        query.Should().Be("?page[limit]=100");
    }
}
