using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Batches;
using Pingen.Client.Batches.ValueTypes;
using Pingen.Client.Common.Json;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Tests.Common.Json;

public class EnumValueMirrorTests
{
    [Theory]
    [InlineData(typeof(AddressPosition), typeof(AddressPositionValue))]
    [InlineData(typeof(PrintMode), typeof(PrintModeValue))]
    [InlineData(typeof(PrintSpectrum), typeof(PrintSpectrumValue))]
    [InlineData(typeof(DeliveryProduct), typeof(DeliveryProductValue))]
    [InlineData(typeof(BatchDeliveryProduct), typeof(DeliveryProductValue))]
    [InlineData(typeof(BatchIcon), typeof(BatchIconValue))]
    [InlineData(typeof(PaperType), typeof(PaperTypeValue))]
    [InlineData(typeof(BatchChannelType), typeof(BatchChannelTypeValue))]
    public void When_an_enum_is_serialized_every_wire_name_is_declared_on_its_value_class(Type enumType, Type valueType)
    {
        // Act
        var written = WireNames(enumType);

        // Assert
        written.Should().BeSubsetOf(Constants(valueType));
    }

    [Theory]
    [InlineData(typeof(AddressPosition), typeof(AddressPositionValue))]
    [InlineData(typeof(PrintMode), typeof(PrintModeValue))]
    [InlineData(typeof(PrintSpectrum), typeof(PrintSpectrumValue))]
    [InlineData(typeof(BatchDeliveryProduct), typeof(DeliveryProductValue))]
    [InlineData(typeof(BatchIcon), typeof(BatchIconValue))]
    [InlineData(typeof(PaperType), typeof(PaperTypeValue))]
    [InlineData(typeof(BatchChannelType), typeof(BatchChannelTypeValue))]
    public void When_a_value_class_mirrors_an_enum_it_declares_one_distinct_constant_per_member(Type enumType, Type valueType)
    {
        // Act
        var constants = Constants(valueType);

        // Assert
        constants.Should().HaveCount(Enum.GetValues(enumType).Length);
        constants.Should().OnlyHaveUniqueItems();
    }

    private static IReadOnlyList<string> Constants(Type valueType) =>
        valueType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field is { IsLiteral: true, IsInitOnly: false })
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

    private static IReadOnlyList<string> WireNames(Type enumType) =>
        Enum.GetValues(enumType)
            .Cast<object>()
            .Select(value => JsonSerializer.Serialize(value, enumType, PingenJson.Options).Trim('"'))
            .ToList();
}
