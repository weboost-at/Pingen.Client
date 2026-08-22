using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Common.JsonApi;
using Pingen.Client.Deliveries.Letters;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Tests.Deliveries.Letters;

public class LetterTests
{
    [Fact]
    public void When_a_letter_arrives_Deserialize_maps_every_attribute_relationship_and_ability()
    {
        // Act
        var letter = JsonSerializer.Deserialize<SingleDocument<Letter>>($$"""{ "data": {{LetterJson}} }""", PingenJson.Options)!.Data;

        // Assert
        letter.Id.Should().Be(Guid.Parse("2a4c9e77-2222-4000-8000-000000000002"));
        letter.Type.Should().Be("letters");
        letter.Attributes.Status.Should().Be("sent");
        letter.Attributes.FileOriginalName.Should().Be("lörem.pdf");
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
        letter.Attributes.Fonts.Should().HaveCount(2).And.Contain(font => font.Name == "Helvetica" && font.IsEmbedded);
        letter.Attributes.Source.Should().Be("api");
        letter.Attributes.TrackingNumber.Should().Be("98.1234.11");
        letter.Attributes.SubmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        letter.Attributes.CreatedAt.Should().Be(new DateTimeOffset(2020, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        letter.Attributes.UpdatedAt.Should().Be(new DateTimeOffset(2020, 11, 20, 10, 0, 0, TimeSpan.FromHours(1)));
        letter.Relationships!.Organisation!.Data!.Id.Should().Be("6c3d1f0a-1111-4000-8000-000000000001");
        letter.Relationships.Organisation.Data.Type.Should().Be("organisations");
        letter.Relationships.Batch!.Data.Should().BeNull();
        letter.Relationships.Events!.Count.Should().Be(3);
        letter.Relationships.Events.Href.Should().Be("https://api.pingen.com/organisations/6c3d1f0a/deliveries/letters/2a4c9e77/events");
        letter.Links!.Self.Should().Be("https://api.pingen.com/organisations/6c3d1f0a/deliveries/letters/2a4c9e77");
        letter.Meta!.Abilities.Should().Contain(new KeyValuePair<string, string>("send-simplex", "permission"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void When_the_embedded_flag_arrives_as_a_boolean_or_a_number_Deserialize_maps_it_the_same_way(string wire, bool expected)
    {
        // Act
        var font = JsonSerializer.Deserialize<LetterFont>($$"""{ "name": "Helvetica", "is_embedded": {{wire}}, "kerning": "unknown" }""", PingenJson.Options)!;

        // Assert
        font.Name.Should().Be("Helvetica");
        font.IsEmbedded.Should().Be(expected);
    }

    [Theory]
    [InlineData(PaperType.Normal, "normal")]
    [InlineData(PaperType.Qr, "qr")]
    [InlineData(PaperType.SepaAt, "sepa_at")]
    [InlineData(PaperType.SepaDe, "sepa_de")]
    public void When_a_paper_type_is_written_Serialize_uses_its_wire_name(PaperType paperType, string wire)
    {
        // Act
        var json = JsonSerializer.Serialize(paperType, PingenJson.Options);

        // Assert
        json.Should().Be($"\"{wire}\"");
    }

    [Fact]
    public void When_only_the_required_members_are_set_Serialize_omits_every_optional_one_and_the_preset()
    {
        // Arrange
        var options = new LetterCreateOptions
        {
            FileOriginalName = "lörem.pdf",
            AutoSend = false,
            PresetId = Guid.Parse("6c3d1f0a-1111-4000-8000-000000000001"),
        };

        // Act
        var json = JsonSerializer.Serialize(options, PingenJson.Options);

        // Assert
        json.Should().Be("""{"file_original_name":"lörem.pdf","auto_send":false}""");
    }

    [Fact]
    public void When_address_blocks_are_given_Serialize_writes_the_shape_the_api_documents()
    {
        // Arrange
        var options = new LetterSendOptions
        {
            DeliveryProduct = DeliveryProduct.Registered,
            PrintMode = PrintMode.Duplex,
            PrintSpectrum = PrintSpectrum.Grayscale,
            MetaData = new()
            {
                Recipient = new() { Name = "Alex Meier", Street = "Example street", Number = "50A", Zip = "8051", City = "Zürich", Country = "CH" },
                Sender = new() { Name = "Pingen AG", PoBox = "Postfach 100", Zip = "8000", City = "Zürich", Country = "CH" },
            },
        };

        // Act
        var json = JsonSerializer.Serialize(options, PingenJson.Options);

        // Assert
        json.Should().Be(
            """{"delivery_product":"registered","print_mode":"duplex","print_spectrum":"grayscale","meta_data":{"recipient":{"name":"Alex Meier","street":"Example street","number":"50A","zip":"8051","city":"Zürich","country":"CH"},"sender":{"name":"Pingen AG","pobox":"Postfach 100","zip":"8000","city":"Zürich","country":"CH"}}}"""
        );
    }

    private const string LetterJson = """
                                      {
                                        "id": "2a4c9e77-2222-4000-8000-000000000002",
                                        "type": "letters",
                                        "attributes": {
                                          "status": "sent",
                                          "file_original_name": "lörem.pdf",
                                          "file_pages": 2,
                                          "address": "Hans Meier\nExample street 4\n8000 Zürich\nSwitzerland",
                                          "address_position": "left",
                                          "country": "CH",
                                          "delivery_product": "fast",
                                          "print_mode": "simplex",
                                          "print_spectrum": "color",
                                          "price_currency": "CHF",
                                          "price_value": 1.25,
                                          "paper_types": ["normal", "qr"],
                                          "fonts": [
                                            { "name": "Helvetica", "is_embedded": true },
                                            { "name": "Helvetica-Bold", "is_embedded": 0 }
                                          ],
                                          "source": "api",
                                          "tracking_number": "98.1234.11",
                                          "submitted_at": "2021-11-19T09:42:48+0100",
                                          "created_at": "2020-11-19T09:42:48+0100",
                                          "updated_at": "2020-11-20T10:00:00+0100",
                                          "cover_page": "an attribute this client does not know"
                                        },
                                        "relationships": {
                                          "organisation": {
                                            "links": { "related": "https://api.pingen.com/organisations/6c3d1f0a" },
                                            "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "organisations" }
                                          },
                                          "batch": { "data": null },
                                          "events": {
                                            "links": {
                                              "related": {
                                                "href": "https://api.pingen.com/organisations/6c3d1f0a/deliveries/letters/2a4c9e77/events",
                                                "meta": { "count": 3 }
                                              }
                                            }
                                          }
                                        },
                                        "links": { "self": "https://api.pingen.com/organisations/6c3d1f0a/deliveries/letters/2a4c9e77" },
                                        "meta": { "abilities": { "self": { "cancel": "ok", "delete": "state", "send-simplex": "permission" } } }
                                      }
                                      """;
}
