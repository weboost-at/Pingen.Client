using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common;

public class PingenFilterTests : UnitTestBase
{
    [Theory]
    [InlineData("sent", """{"status":"sent"}""")]
    [InlineData("Zürich, Grüezi", """{"status":"Zürich, Grüezi"}""")]
    [InlineData("O\"Brien", """{"status":"O\"Brien"}""")]
    [InlineData("C:\\Briefe\tneu", """{"status":"C:\\Briefe\tneu"}""")]
    [InlineData("<b>Müller & Söhne</b>", """{"status":"<b>Müller & Söhne</b>"}""")]
    [InlineData("", """{"status":""}""")]
    public void When_a_value_is_matched_Where_writes_it_as_a_json_string(string value, string json) =>
        PingenFilter.Where("status", value).ToJson().Should().Be(json);

    [Fact]
    public void When_a_text_is_compared_ToJson_prefixes_the_operator_to_it()
    {
        // Act
        string[] rendered =
        [
            PingenFilter.Not("status", "sent").ToJson(),
            PingenFilter.Contains("address", "Bahnhofstrasse").ToJson()
        ];

        // Assert
        rendered.Should().Equal(
            """{"status":"!sent"}""",
            """{"address":"~Bahnhofstrasse"}"""
        );
    }

    [Fact]
    public void When_a_date_is_compared_ToJson_prefixes_the_operator_to_the_iso_date()
    {
        // Arrange
        var date = new DateOnly(2021, 11, 19);

        // Act
        string[] rendered =
        [
            PingenFilter.Where("invoice_date", date).ToJson(),
            PingenFilter.GreaterOrEqual("invoice_date", date).ToJson(),
            PingenFilter.LessOrEqual("invoice_date", date).ToJson()
        ];

        // Assert
        rendered.Should().Equal(
            """{"invoice_date":"2021-11-19"}""",
            """{"invoice_date":">=2021-11-19"}""",
            """{"invoice_date":"<=2021-11-19"}"""
        );
    }

    [Fact]
    public void When_a_timestamp_is_compared_ToJson_prefixes_the_operator_to_the_wire_format()
    {
        // Arrange
        var instant = new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1));

        // Act
        string[] rendered =
        [
            PingenFilter.GreaterThan("created_at", instant).ToJson(),
            PingenFilter.LessThan("created_at", instant).ToJson()
        ];

        // Assert
        rendered.Should().Equal(
            """{"created_at":">2021-11-19T09:42:48+01:00"}""",
            """{"created_at":"<2021-11-19T09:42:48+01:00"}"""
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
