using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common;

public class PingenFilterTests
{
    [Fact]
    public void When_a_comparison_filter_is_built_ToJson_prepends_the_operator_to_the_value()
    {
        // Arrange
        var date = new DateOnly(2021, 11, 19);
        var instant = new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1));

        // Act
        var rendered = new[]
        {
            PingenFilter.Where("status", "sent"),
            PingenFilter.Where("name", "Zürich, Grüezi"),
            PingenFilter.Where("name", "O\"Brien"),
            PingenFilter.Where("invoice_date", date),
            PingenFilter.Not("status", "sent"),
            PingenFilter.Contains("address", "Bahnhofstrasse"),
            PingenFilter.GreaterThan("created_at", instant),
            PingenFilter.GreaterOrEqual("invoice_date", date),
            PingenFilter.LessThan("created_at", instant),
            PingenFilter.LessOrEqual("invoice_date", date),
        }.Select(filter => filter.ToJson());

        // Assert
        rendered.Should().Equal(
            """{"status":"sent"}""",
            """{"name":"Zürich, Grüezi"}""",
            """{"name":"O\"Brien"}""",
            """{"invoice_date":"2021-11-19"}""",
            """{"status":"!sent"}""",
            """{"address":"~Bahnhofstrasse"}""",
            """{"created_at":">2021-11-19T09:42:48+01:00"}""",
            """{"invoice_date":">=2021-11-19"}""",
            """{"created_at":"<2021-11-19T09:42:48+01:00"}""",
            """{"invoice_date":"<=2021-11-19"}"""
        );
    }

    [Fact]
    public void When_filters_are_combined_ToJson_nests_them_under_the_combinator()
    {
        // Arrange
        var filter = PingenFilter.And(
            PingenFilter.Where("status", "sent"),
            PingenFilter.Or(
                PingenFilter.GreaterThan("created_at", new DateOnly(2021, 11, 19)),
                PingenFilter.Raw("""{"country":"CH"}""")
            )
        );

        // Act
        var json = filter.ToJson();

        // Assert
        json.Should().Be("""{"and":[{"status":"sent"},{"or":[{"created_at":">2021-11-19"},{"country":"CH"}]}]}""");
    }
}
