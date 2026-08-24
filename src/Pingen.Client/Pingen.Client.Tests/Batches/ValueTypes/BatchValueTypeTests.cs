using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Batches;
using Pingen.Client.Batches.ValueTypes;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Tests.Batches.ValueTypes;

public class BatchValueTypeTests
{
    [Theory]
    [InlineData(BatchIcon.Campaign, "\"campaign\"")]
    [InlineData(BatchIcon.Megaphone, "\"megaphone\"")]
    [InlineData(BatchIcon.WaveHand, "\"wave-hand\"")]
    [InlineData(BatchIcon.Flash, "\"flash\"")]
    [InlineData(BatchIcon.Rocket, "\"rocket\"")]
    [InlineData(BatchIcon.Bell, "\"bell\"")]
    [InlineData(BatchIcon.PercentTag, "\"percent-tag\"")]
    [InlineData(BatchIcon.PercentBadge, "\"percent-badge\"")]
    [InlineData(BatchIcon.Present, "\"present\"")]
    [InlineData(BatchIcon.Receipt, "\"receipt\"")]
    [InlineData(BatchIcon.Document, "\"document\"")]
    [InlineData(BatchIcon.Information, "\"information\"")]
    [InlineData(BatchIcon.Calendar, "\"calendar\"")]
    [InlineData(BatchIcon.Newspaper, "\"newspaper\"")]
    [InlineData(BatchIcon.Crown, "\"crown\"")]
    [InlineData(BatchIcon.Virus, "\"virus\"")]
    [InlineData(BatchGroupingType.Zip, "\"zip\"")]
    [InlineData(BatchGroupingType.Merge, "\"merge\"")]
    [InlineData(BatchSplitType.File, "\"file\"")]
    [InlineData(BatchSplitType.Page, "\"page\"")]
    [InlineData(BatchSplitType.Custom, "\"custom\"")]
    [InlineData(BatchSplitType.QrInvoice, "\"qr_invoice\"")]
    [InlineData(BatchSplitPosition.FirstPage, "\"first_page\"")]
    [InlineData(BatchSplitPosition.LastPage, "\"last_page\"")]
    [InlineData(BatchChannelType.Post, "\"post\"")]
    [InlineData(BatchChannelType.Ebill, "\"ebill\"")]
    [InlineData(BatchChannelType.Email, "\"email\"")]
    [InlineData(BatchDeliveryProduct.Fast, "\"fast\"")]
    [InlineData(BatchDeliveryProduct.Cheap, "\"cheap\"")]
    [InlineData(BatchDeliveryProduct.Bulk, "\"bulk\"")]
    [InlineData(BatchDeliveryProduct.Premium, "\"premium\"")]
    [InlineData(BatchDeliveryProduct.Registered, "\"registered\"")]
    [InlineData(BatchDeliveryProduct.ElectronicEmail, "\"electronic_email\"")]
    [InlineData(BatchDeliveryProduct.ElectronicEbill, "\"electronic_ebill\"")]
    public void When_a_batch_value_type_crosses_the_wire_it_round_trips_through_its_declared_name(object value, string wire)
    {
        // Act
        var written = JsonSerializer.Serialize(value, PingenJson.Options);
        var read = JsonSerializer.Deserialize(wire, value.GetType(), PingenJson.Options);

        // Assert
        written.Should().Be(wire);
        read.Should().Be(value);
    }

    [Theory]
    [InlineData("\"wave_hand\"")]
    [InlineData("\"WaveHand\"")]
    public void When_the_icon_is_not_written_in_kebab_case_Deserialize_throws(string wire)
    {
        // Act
        var read = () => JsonSerializer.Deserialize<BatchIcon>(wire, PingenJson.Options);

        // Assert
        read.Should().Throw<JsonException>();
    }

    [Fact]
    public void When_every_icon_is_written_the_wire_names_stay_lowercase_and_free_of_underscores()
    {
        // Act
        var written = Enum.GetValues<BatchIcon>().Select(icon => JsonSerializer.Serialize(icon, PingenJson.Options).Trim('"')).ToList();

        // Assert
        written.Should().HaveCount(16);
        written.Should().OnlyContain(name => name == name.ToLowerInvariant() && !name.Contains('_'));
        written.Should().Contain(["wave-hand", "percent-tag", "percent-badge"]);
    }
}
