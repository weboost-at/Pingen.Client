using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Tests.Common.Json;

public class PingenEnumConverterTests
{
    [Theory]
    [InlineData(TestIcon.WaveHand, "\"wave-hand\"")]
    [InlineData(TestIcon.PercentTag, "\"percent-tag\"")]
    [InlineData(TestIcon.QrInvoice, "\"qr_invoice\"")]
    [InlineData(TestIcon.Campaign, "\"Campaign\"")]
    public void When_a_member_declares_a_wire_name_the_enum_round_trips_through_it(TestIcon icon, string wire)
    {
        // Act
        var written = JsonSerializer.Serialize(icon, PingenJson.Options);
        var read = JsonSerializer.Deserialize<TestIcon>(wire, PingenJson.Options);

        // Assert
        written.Should().Be(wire);
        read.Should().Be(icon);
    }

    [Theory]
    [InlineData("\"waveHand\"")]
    [InlineData("\"wave_hand\"")]
    [InlineData("0")]
    public void When_the_wire_value_is_not_a_declared_name_Read_throws(string wire)
    {
        // Act
        var read = () => JsonSerializer.Deserialize<TestIcon>(wire, PingenJson.Options);

        // Assert
        read.Should().Throw<JsonException>();
    }

    public enum TestIcon
    {
        [JsonStringEnumMemberName("wave-hand")]
        WaveHand,

        [JsonStringEnumMemberName("percent-tag")]
        PercentTag,

        [JsonStringEnumMemberName("qr_invoice")]
        QrInvoice,

        Campaign,
    }
}
