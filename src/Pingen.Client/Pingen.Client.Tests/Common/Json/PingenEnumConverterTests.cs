using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Tests.Common.Json;

public class PingenEnumConverterTests : UnitTestBase
{
    [Theory]
    [InlineData(TestIcon.WaveHand, "\"wave-hand\"")]
    [InlineData(TestIcon.PercentTag, "\"percent-tag\"")]
    [InlineData(TestIcon.QrInvoice, "\"qr_invoice\"")]
    [InlineData(TestIcon.Campaign, "\"Campaign\"")]
    public void A_Member_Round_Trips_Through_Its_Wire_Name(TestIcon icon, string wire)
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
    [InlineData("\"WaveHand\"")]
    [InlineData("\"wavé-hand\"")]
    [InlineData("\"\"")]
    [InlineData("null")]
    [InlineData("\"1\"")]
    [InlineData("0")]
    public void Given_An_Undeclared_Wire_Value_Deserialize_Throws(string wire)
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

        Campaign
    }
}
