using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Batches;
using Pingen.Client.Batches.ValueTypes;
using Pingen.Client.Common.Json;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Tests.Batches.ValueTypes;

public class BatchValueTypeTests : UnitTestBase
{
    [Theory]
    [InlineData(BatchIcon.Campaign, "campaign")]
    [InlineData(BatchIcon.Megaphone, "megaphone")]
    [InlineData(BatchIcon.WaveHand, "wave-hand")]
    [InlineData(BatchIcon.Flash, "flash")]
    [InlineData(BatchIcon.Rocket, "rocket")]
    [InlineData(BatchIcon.Bell, "bell")]
    [InlineData(BatchIcon.PercentTag, "percent-tag")]
    [InlineData(BatchIcon.PercentBadge, "percent-badge")]
    [InlineData(BatchIcon.Present, "present")]
    [InlineData(BatchIcon.Receipt, "receipt")]
    [InlineData(BatchIcon.Document, "document")]
    [InlineData(BatchIcon.Information, "information")]
    [InlineData(BatchIcon.Calendar, "calendar")]
    [InlineData(BatchIcon.Newspaper, "newspaper")]
    [InlineData(BatchIcon.Crown, "crown")]
    [InlineData(BatchIcon.Virus, "virus")]
    [InlineData(BatchGroupingType.Zip, "zip")]
    [InlineData(BatchGroupingType.Merge, "merge")]
    [InlineData(BatchSplitType.File, "file")]
    [InlineData(BatchSplitType.Page, "page")]
    [InlineData(BatchSplitType.Custom, "custom")]
    [InlineData(BatchSplitType.QrInvoice, "qr_invoice")]
    [InlineData(BatchSplitPosition.FirstPage, "first_page")]
    [InlineData(BatchSplitPosition.LastPage, "last_page")]
    [InlineData(BatchChannelType.Post, "post")]
    [InlineData(BatchChannelType.Ebill, "ebill")]
    [InlineData(BatchChannelType.Email, "email")]
    [InlineData(BatchDeliveryProduct.Fast, "fast")]
    [InlineData(BatchDeliveryProduct.Cheap, "cheap")]
    [InlineData(BatchDeliveryProduct.Bulk, "bulk")]
    [InlineData(BatchDeliveryProduct.Premium, "premium")]
    [InlineData(BatchDeliveryProduct.Registered, "registered")]
    [InlineData(BatchDeliveryProduct.ElectronicEmail, "electronic_email")]
    [InlineData(BatchDeliveryProduct.ElectronicEbill, "electronic_ebill")]
    public void A_Value_Round_Trips_Through_Its_Declared_Name(object value, string wire)
    {
        // Arrange
        var json = $"\"{wire}\"";

        // Act
        var written = JsonSerializer.Serialize(value, PingenJson.Options);
        var read = JsonSerializer.Deserialize(json, value.GetType(), PingenJson.Options);

        // Assert
        written.Should().Be(json);
        read.Should().Be(value);
    }

    [Theory]
    [InlineData("wave_hand")]
    [InlineData("WaveHand")]
    [InlineData("Wave-Hand")]
    [InlineData("wave hand")]
    [InlineData("wave–hand")]
    [InlineData("wavehand")]
    public void Given_An_Icon_Not_In_Kebab_Case_Deserialize_Throws(string wire)
    {
        // Act
        var read = () => JsonSerializer.Deserialize<BatchIcon>($"\"{wire}\"", PingenJson.Options);

        // Assert
        read.Should().Throw<JsonException>();
    }

    [Fact]
    public void Every_Icon_Wire_Name_Is_Lowercase_And_Free_Of_Underscores()
    {
        // Act
        var written = Enum.GetValues<BatchIcon>().Select(icon => JsonSerializer.Serialize(icon, PingenJson.Options).Trim('"')).ToList();

        // Assert
        written.Should().HaveCount(16);
        written.Should().OnlyContain(name => name == name.ToLowerInvariant() && !name.Contains('_'));
        written.Should().Contain(["wave-hand", "percent-tag", "percent-badge"]);
    }

    [Theory]
    [InlineData(DeliveryProduct.Fast, BatchDeliveryProduct.Fast)]
    [InlineData(DeliveryProduct.Cheap, BatchDeliveryProduct.Cheap)]
    [InlineData(DeliveryProduct.Bulk, BatchDeliveryProduct.Bulk)]
    [InlineData(DeliveryProduct.Premium, BatchDeliveryProduct.Premium)]
    [InlineData(DeliveryProduct.Registered, BatchDeliveryProduct.Registered)]
    public void Given_A_Postal_Product_Post_Maps_It(DeliveryProduct product, BatchDeliveryProduct expected) =>
        BatchSendOptions.Post(product, PrintMode.Simplex, PrintSpectrum.Color).Product.Should().Be(expected);

    [Theory]
    [InlineData((DeliveryProduct)(-1))]
    [InlineData((DeliveryProduct)5)]
    [InlineData((DeliveryProduct)99)]
    [InlineData((DeliveryProduct)int.MaxValue)]
    public void Given_A_Product_Without_Batch_Counterpart_Post_Throws(DeliveryProduct product)
    {
        // Act
        var post = () => BatchSendOptions.Post(product, PrintMode.Simplex, PrintSpectrum.Color);

        // Assert
        post.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("product")
            .WithMessage("The post channel of a batch knows no such delivery product.*");
    }
}
