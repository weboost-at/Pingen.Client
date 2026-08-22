using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Tests.Deliveries.ValueTypes;

public class DeliveryValueTypeTests
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
    public void When_a_delivery_value_type_crosses_the_wire_it_round_trips_through_its_lowercase_name(object value, string wire)
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
    [InlineData("0")]
    public void When_the_wire_value_is_not_a_declared_name_Deserialize_throws(string wire)
    {
        // Act
        var read = () => JsonSerializer.Deserialize<DeliveryProduct>(wire, PingenJson.Options);

        // Assert
        read.Should().Throw<JsonException>();
    }
}
