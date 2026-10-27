using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Tests.Deliveries.ValueTypes;

public class DeliveryValueTypeTests : UnitTestBase
{
    [Theory]
    [InlineData(DeliveryProduct.Fast, "\"fast\"")]
    [InlineData(DeliveryProduct.Cheap, "\"cheap\"")]
    [InlineData(DeliveryProduct.Bulk, "\"bulk\"")]
    [InlineData(DeliveryProduct.Premium, "\"premium\"")]
    [InlineData(DeliveryProduct.Registered, "\"registered\"")]
    [InlineData(PrintMode.Simplex, "\"simplex\"")]
    [InlineData(PrintMode.Duplex, "\"duplex\"")]
    [InlineData(PrintSpectrum.Color, "\"color\"")]
    [InlineData(PrintSpectrum.Grayscale, "\"grayscale\"")]
    [InlineData(AddressPosition.Left, "\"left\"")]
    [InlineData(AddressPosition.Right, "\"right\"")]
    public void A_Value_Round_Trips_Through_Its_Lowercase_Name(object value, string wire)
    {
        // Act
        var written = JsonSerializer.Serialize(value, PingenJson.Options);
        var read = JsonSerializer.Deserialize(wire, value.GetType(), PingenJson.Options);

        // Assert
        written.Should().Be(wire);
        read.Should().Be(value);
    }

    [Theory]
    [InlineData("\"Fast\"")]
    [InlineData("\"FAST\"")]
    [InlineData("\"fäst\"")]
    [InlineData("\"electronic_email\"")]
    [InlineData("\"\"")]
    [InlineData("null")]
    [InlineData("\"0\"")]
    [InlineData("0")]
    public void Given_An_Undeclared_Wire_Value_Deserialize_Throws(string wire)
    {
        // Act
        var read = () => JsonSerializer.Deserialize<DeliveryProduct>(wire, PingenJson.Options);

        // Assert
        read.Should().Throw<JsonException>();
    }
}
