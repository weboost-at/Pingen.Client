using System.Buffers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Tests.Common.Json;

public class PingenDateTimeConverterTests : UnitTestBase
{
    [Theory]
    [InlineData("2021-11-19T09:42:48+0100", 60)]
    [InlineData("2021-11-19T09:42:48+01:00", 60)]
    [InlineData("2021-11-19T09:42:48+0000", 0)]
    [InlineData("2021-11-19T09:42:48Z", 0)]
    [InlineData("2021-11-19T09:42:48-0530", -330)]
    [InlineData("2021-11-19T09:42:48+0545", 345)]
    [InlineData("2021-11-19T09:42:48+1400", 840)]
    public void When_the_offset_omits_its_colon_Read_still_parses_the_timestamp(string wire, int offsetMinutes)
    {
        // Arrange
        var expected = new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromMinutes(offsetMinutes));

        // Act
        var parsed = JsonSerializer.Deserialize<DateTimeOffset>($"\"{wire}\"", PingenJson.Options);

        // Assert
        parsed.Should().Be(expected);
        parsed.Offset.Should().Be(expected.Offset);
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

    // JsonSerializer writes a null itself and never hands it to the converter - only a direct call reaches it
    [Fact]
    public void When_a_nullable_timestamp_has_no_value_Write_writes_json_null()
    {
        // Arrange
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        // Act
        new PingenNullableDateTimeConverter().Write(writer, null, PingenJson.Options);
        writer.Flush();

        // Assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("null");
    }
}
