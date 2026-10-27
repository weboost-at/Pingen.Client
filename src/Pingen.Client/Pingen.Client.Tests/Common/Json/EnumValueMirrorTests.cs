using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Batches;
using Pingen.Client.Batches.ValueTypes;
using Pingen.Client.Common.Json;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Tests.Common.Json;

public class EnumValueMirrorTests : UnitTestBase
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
    public void Every_Wire_Name_Of_An_Enum_Is_A_Constant_Of_Its_Value_Class(Type enumType, Type valueType)
    {
        // Act
        var written = Enum.GetValues(enumType)
            .Cast<object>()
            .Select(value => JsonSerializer.Serialize(value, enumType, PingenJson.Options).Trim('"'))
            .ToList();

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
    public void A_Value_Class_Declares_Each_Member_Of_Its_Enum_Once(Type enumType, Type valueType)
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
}
