using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Tests.Common.Json;

public class PingenDateTimeConverterTests
{
    [Theory]
    [InlineData("2021-11-19T09:42:48+0100", 1)]
    [InlineData("2021-11-19T09:42:48+01:00", 1)]
    [InlineData("2021-11-19T09:42:48+0000", 0)]
    [InlineData("2021-11-19T09:42:48Z", 0)]
    public void When_the_offset_omits_its_colon_Read_still_parses_the_timestamp(string wire, int offsetHours)
    {
        // Arrange
        var expected = new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(offsetHours));

        // Act
        var parsed = JsonSerializer.Deserialize<DateTimeOffset>($"\"{wire}\"", PingenJson.Options);

        // Assert
        parsed.Should().Be(expected);
        parsed.Offset.Should().Be(TimeSpan.FromHours(offsetHours));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    public void When_a_nullable_timestamp_is_absent_Read_returns_null(string wire) =>
        JsonSerializer.Deserialize<DateTimeOffset?>(wire, PingenJson.Options).Should().BeNull();

    [Fact]
    public void When_a_timestamp_is_written_Write_uses_the_pingen_wire_format()
    {
        // Arrange
        var instant = new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1));

        // Act
        var written = JsonSerializer.Serialize(instant, PingenJson.Options);
        var roundTripped = JsonSerializer.Deserialize<DateTimeOffset>(written, PingenJson.Options);

        // Assert
        written.Should().Be("\"2021-11-19T09:42:48+01:00\"");
        roundTripped.Should().Be(instant);
    }
}
