using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Common.JsonApi;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Deliveries.ValueTypes;
using Pingen.Client.Tests.Deliveries.Letters.Support;

namespace Pingen.Client.Tests.Deliveries.Letters;

public class LetterTests : LetterTestBase
{
    [Fact]
    public void Deserialize_Maps_Every_Attribute_Relationship_And_Ability()
    {
        // Act
        var letter = JsonSerializer.Deserialize<SingleDocument<Letter>>(LetterDocument(), PingenJson.Options)!.Data;

        // Assert
        letter.Id.Should().Be(Letter.Id);
        letter.Type.Should().Be("letters");
        letter.Attributes.Status.Should().Be("sent");
        letter.Attributes.FileOriginalName.Should().Be(Letter.FileOriginalName);
        letter.Attributes.FilePages.Should().Be(2);
        letter.Attributes.Address.Should().Be("Hans Meier\nExample street 4\n8000 Zürich\nSwitzerland");
        letter.Attributes.AddressPosition.Should().Be("left");
        letter.Attributes.Country.Should().Be("CH");
        letter.Attributes.DeliveryProduct.Should().Be("fast");
        letter.Attributes.PrintMode.Should().Be("simplex");
        letter.Attributes.PrintSpectrum.Should().Be("color");
        letter.Attributes.PriceCurrency.Should().Be("CHF");
        letter.Attributes.PriceValue.Should().Be(1.25m);
        letter.Attributes.PaperTypes.Should().Equal("normal", "qr");
        letter.Attributes.Source.Should().Be("api");
        letter.Attributes.TrackingNumber.Should().Be("98.1234.11");
        letter.Attributes.SubmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        letter.Attributes.CreatedAt.Should().Be(new DateTimeOffset(2020, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        letter.Attributes.UpdatedAt.Should().Be(new DateTimeOffset(2020, 11, 20, 10, 0, 0, TimeSpan.FromHours(1)));

        letter.Attributes.Fonts.Should().HaveCount(2);
        letter.Attributes.Fonts[0].Name.Should().Be("Helvetica");
        letter.Attributes.Fonts[0].IsEmbedded.Should().BeTrue();
        letter.Attributes.Fonts[1].IsEmbedded.Should().BeFalse();

        letter.Relationships!.Batch!.Data.Should().BeNull();
        letter.Relationships.Events!.Count.Should().Be(3);
        letter.Relationships.Events.Href.Should().Be($"https://api.pingen.com{LetterPath}/events");
        letter.Relationships.Organisation!.Data!.Type.Should().Be("organisations");
        letter.Relationships.Organisation.Data.Id.Should().Be(Organisation.Id.ToString());

        letter.Links!.Self.Should().Be($"https://api.pingen.com{LetterPath}");
        letter.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("send-simplex", "permission"));
    }

    [Fact]
    public void Given_An_Unsent_Letter_Deserialize_Leaves_Its_Delivery_And_Price_Null()
    {
        // Act
        var letter = JsonSerializer.Deserialize<SingleDocument<Letter>>(UnsentLetterDocument("valid", "1"), PingenJson.Options)!.Data;

        // Assert
        letter.Attributes.FilePages.Should().Be(1);
        letter.Attributes.PrintMode.Should().BeNull();
        letter.Attributes.PriceValue.Should().BeNull();
        letter.Attributes.PrintSpectrum.Should().BeNull();
        letter.Attributes.PriceCurrency.Should().BeNull();
        letter.Attributes.DeliveryProduct.Should().BeNull();
    }

    [Fact]
    public void Given_A_Validating_Letter_Deserialize_Leaves_Its_Page_Count_Null()
    {
        // Act
        var letter = JsonSerializer.Deserialize<SingleDocument<Letter>>(UnsentLetterDocument("validating", "null"), PingenJson.Options)!.Data;

        // Assert
        letter.Attributes.FilePages.Should().BeNull();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("-1", true)]
    public void Given_A_Numeric_Embedded_Flag_Deserialize_Maps_It_Like_A_Boolean(string wire, bool expected)
    {
        // Act
        var font = JsonSerializer.Deserialize<LetterFont>(
            $$"""{ "name": "Helvetica", "is_embedded": {{wire}}, "kerning": "unknown" }""",
            PingenJson.Options
        )!;

        // Assert
        font.Name.Should().Be("Helvetica");
        font.IsEmbedded.Should().Be(expected);
    }

    [Theory]
    [InlineData(PaperType.Normal, "normal")]
    [InlineData(PaperType.Qr, "qr")]
    [InlineData(PaperType.SepaAt, "sepa_at")]
    [InlineData(PaperType.SepaDe, "sepa_de")]
    public void Serialize_Writes_The_Wire_Name_Of_A_Paper_Type(PaperType paperType, string wire) =>
        JsonSerializer.Serialize(paperType, PingenJson.Options).Should().Be($"\"{wire}\"");

    [Fact]
    public void Given_Only_Required_Members_Serialize_Omits_Every_Optional_One_And_The_Preset()
    {
        // Arrange
        var options = new LetterCreateOptions
        {
            FileOriginalName = Letter.FileOriginalName,
            AutoSend = false,
            PresetId = Letter.PresetId
        };

        // Act
        var json = JsonSerializer.Serialize(options, PingenJson.Options);

        // Assert
        json.Should().Be($$"""{"file_original_name":"{{Letter.FileOriginalName}}","auto_send":false}""");
    }

    [Fact]
    public void Given_Address_Blocks_Serialize_Writes_The_Shape_The_Api_Documents()
    {
        // Arrange
        var options = new LetterSendOptions
        {
            DeliveryProduct = DeliveryProduct.Registered,
            PrintMode = PrintMode.Duplex,
            PrintSpectrum = PrintSpectrum.Grayscale,
            MetaData = new()
            {
                Recipient = new()
                {
                    Name = "Alex Meier",
                    Street = "Example street",
                    Number = "50A",
                    Zip = "8051",
                    City = "Zürich",
                    Country = "CH"
                },
                Sender = new()
                {
                    Name = "Pingen AG",
                    PoBox = "Postfach 100",
                    Zip = "8000",
                    City = "Zürich",
                    Country = "CH"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(options, PingenJson.Options);

        // Assert
        json.Should().Be(
            """{"delivery_product":"registered","print_mode":"duplex","print_spectrum":"grayscale","meta_data":{"recipient":{"name":"Alex Meier","street":"Example street","number":"50A","zip":"8051","city":"Zürich","country":"CH"},"sender":{"name":"Pingen AG","pobox":"Postfach 100","zip":"8000","city":"Zürich","country":"CH"}}}"""
        );
    }

    private string UnsentLetterDocument(string status, string filePages) =>
        $$"""
          {
            "data": {
              "id": "{{Letter.Id}}",
              "type": "letters",
              "attributes": {
                "status": "{{status}}",
                "file_original_name": "{{Letter.FileOriginalName}}",
                "file_pages": {{filePages}},
                "address": "Hans Meier\nExample street 4\n8000 Zürich\nSwitzerland",
                "address_position": "left",
                "country": "CH",
                "delivery_product": null,
                "print_mode": null,
                "print_spectrum": null,
                "price_currency": null,
                "price_value": null,
                "paper_types": ["normal"],
                "fonts": [{ "name": "LiberationSans", "is_embedded": true }],
                "source": "api",
                "tracking_number": null,
                "submitted_at": null,
                "created_at": "2026-10-11T16:14:40+0200",
                "updated_at": "2026-10-11T16:14:43+0200"
              }
            }
          }
          """;
}
