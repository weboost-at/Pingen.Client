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
    public void Where_Writes_The_Value_As_A_Json_String(string value, string json) =>
        PingenFilter.Where("status", value).ToJson().Should().Be(json);

    [Fact]
    public void Given_A_Text_Comparison_ToJson_Prefixes_The_Operator()
    {
        // Act
        string[] rendered =
        [
            PingenFilter.Not("status", "sent").ToJson(),
            PingenFilter.Contains("address", "Bahnhofstrasse").ToJson(),
            PingenFilter.GreaterThan("file_pages", "2").ToJson(),
            PingenFilter.GreaterOrEqual("file_pages", "2").ToJson(),
            PingenFilter.LessThan("file_pages", "2").ToJson(),
            PingenFilter.LessOrEqual("file_pages", "2").ToJson()
        ];

        // Assert
        rendered.Should().Equal(
            """{"status":"!sent"}""",
            """{"address":"~Bahnhofstrasse"}""",
            """{"file_pages":">2"}""",
            """{"file_pages":">=2"}""",
            """{"file_pages":"<2"}""",
            """{"file_pages":"<=2"}"""
        );
    }

    [Fact]
    public void Given_A_Date_Comparison_ToJson_Prefixes_The_Operator_To_The_Iso_Date()
    {
        // Arrange
        var date = new DateOnly(2021, 11, 19);

        // Act
        string[] rendered =
        [
            PingenFilter.Where("invoice_date", date).ToJson(),
            PingenFilter.GreaterThan("invoice_date", date).ToJson(),
            PingenFilter.GreaterOrEqual("invoice_date", date).ToJson(),
            PingenFilter.LessThan("invoice_date", date).ToJson(),
            PingenFilter.LessOrEqual("invoice_date", date).ToJson()
        ];

        // Assert
        rendered.Should().Equal(
            """{"invoice_date":"2021-11-19"}""",
            """{"invoice_date":">2021-11-19"}""",
            """{"invoice_date":">=2021-11-19"}""",
            """{"invoice_date":"<2021-11-19"}""",
            """{"invoice_date":"<=2021-11-19"}"""
        );
    }

    [Fact]
    public void Given_A_Timestamp_Comparison_ToJson_Prefixes_The_Operator_To_The_Wire_Format()
    {
        // Arrange
        var instant = new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1));

        // Act
        string[] rendered =
        [
            PingenFilter.Where("created_at", instant).ToJson(),
            PingenFilter.GreaterThan("created_at", instant).ToJson(),
            PingenFilter.GreaterOrEqual("created_at", instant).ToJson(),
            PingenFilter.LessThan("created_at", instant).ToJson(),
            PingenFilter.LessOrEqual("created_at", instant).ToJson()
        ];

        // Assert
        rendered.Should().Equal(
            """{"created_at":"2021-11-19T09:42:48+01:00"}""",
            """{"created_at":">2021-11-19T09:42:48+01:00"}""",
            """{"created_at":">=2021-11-19T09:42:48+01:00"}""",
            """{"created_at":"<2021-11-19T09:42:48+01:00"}""",
            """{"created_at":"<=2021-11-19T09:42:48+01:00"}"""
        );
    }

    [Fact]
    public void Given_Combined_Filters_ToJson_Nests_Them_Under_The_Combinator()
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
