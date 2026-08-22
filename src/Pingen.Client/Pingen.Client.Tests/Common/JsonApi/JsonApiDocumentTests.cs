using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Tests.Common.JsonApi;

public class JsonApiDocumentTests
{
    private const string SingleJson = """
                                      {
                                        "data": {
                                          "id": "934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e",
                                          "type": "letters",
                                          "attributes": { "status": "sent", "submitted_at": "2021-11-19T09:42:48+0100" },
                                          "relationships": {
                                            "organisation": {
                                              "links": { "related": "https://api.pingen.com/organisations/6c3d1f0a" },
                                              "data": { "id": "6c3d1f0a", "type": "organisations" }
                                            },
                                            "events": {
                                              "links": { "related": { "href": "https://api.pingen.com/letters/934b6a01/events", "meta": { "count": 3 } } }
                                            }
                                          },
                                          "links": { "self": "https://api.pingen.com/organisations/6c3d1f0a/letters/934b6a01" },
                                          "meta": { "abilities": { "self": { "cancel": "ok", "send": "state", "delete": "permission" } } }
                                        },
                                        "included": [ { "id": "6c3d1f0a", "type": "organisations", "attributes": { "name": "Pingen AG" } } ]
                                      }
                                      """;

    private const string ListJson = """
                                    {
                                      "data": [
                                        { "id": "934b6a01", "type": "letters", "attributes": { "status": "sent", "submitted_at": "2021-11-19T09:42:48+0100" } },
                                        { "id": "a1c8e4d2", "type": "letters", "attributes": { "status": "validating", "submitted_at": "" } }
                                      ],
                                      "links": {
                                        "first": "https://api.pingen.com/letters?page%5Bnumber%5D=1",
                                        "last": "https://api.pingen.com/letters?page%5Bnumber%5D=2",
                                        "prev": null,
                                        "next": "https://api.pingen.com/letters?page%5Bnumber%5D=2",
                                        "self": "https://api.pingen.com/letters?page%5Bnumber%5D=1"
                                      },
                                      "meta": { "current_page": 1, "last_page": 2, "per_page": 2, "from": 1, "to": 2, "total": 3 }
                                    }
                                    """;

    [Fact]
    public void When_a_single_document_arrives_Deserialize_maps_links_relationships_and_abilities()
    {
        // Act
        var document = JsonSerializer.Deserialize<SingleDocument<TestResource>>(SingleJson, PingenJson.Options)!;

        // Assert
        document.Data.Attributes.Status.Should().Be("sent");
        document.Data.Attributes.SubmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        document.Data.Links!.Self.Should().Be("https://api.pingen.com/organisations/6c3d1f0a/letters/934b6a01");
        document.Data.Relationships!.Organisation!.Data!.Id.Should().Be("6c3d1f0a");
        document.Data.Relationships.Organisation.Data.Type.Should().Be("organisations");
        document.Data.Relationships.Organisation.Links!.Related.Should().Be("https://api.pingen.com/organisations/6c3d1f0a");
        document.Data.Relationships.Events!.Count.Should().Be(3);
        document.Data.Relationships.Events.Href.Should().Be("https://api.pingen.com/letters/934b6a01/events");
        document.Data.Meta!.Abilities.Should().Equal(new Dictionary<string, string>
        {
            ["cancel"] = "ok",
            ["send"] = "state",
            ["delete"] = "permission",
        });
        document.Included!.Should().ContainSingle().Which.GetProperty("type").GetString().Should().Be("organisations");
    }

    [Fact]
    public void When_a_list_document_arrives_Deserialize_maps_the_page_and_leaves_item_meta_null()
    {
        // Act
        var document = JsonSerializer.Deserialize<ListDocument<TestResource>>(ListJson, PingenJson.Options)!;

        // Assert
        document.Data.Should().HaveCount(2);
        document.Data[1].Attributes.SubmittedAt.Should().BeNull();
        document.Data[0].Meta.Should().BeNull();
        document.Meta!.CurrentPage.Should().Be(1);
        document.Meta.LastPage.Should().Be(2);
        document.Meta.PerPage.Should().Be(2);
        document.Meta.From.Should().Be(1);
        document.Meta.To.Should().Be(2);
        document.Meta.Total.Should().Be(3);
        document.Links!.Prev.Should().BeNull();
        document.Links.Next.Should().Be("https://api.pingen.com/letters?page%5Bnumber%5D=2");
    }

    [Fact]
    public void When_a_create_document_is_built_Serialize_writes_the_envelope_without_an_id()
    {
        // Arrange
        var document = RequestDocument.For(
            type: "letters",
            attributes: new TestAttributes { Status = "sent" },
            presetId: Guid.Parse("2c3d1f0a-0000-4000-8000-000000000001")
        );

        // Act
        var json = JsonSerializer.Serialize(document, PingenJson.Options);

        // Assert
        json.Should().Be("""{"data":{"type":"letters","attributes":{"status":"sent"},"relationships":{"preset":{"data":{"id":"2c3d1f0a-0000-4000-8000-000000000001","type":"presets"}}}}}""");
    }

    [Fact]
    public void When_a_send_document_is_built_Serialize_repeats_the_path_id_in_the_body()
    {
        // Arrange
        var document = RequestDocument.For(
            type: "letters",
            attributes: new TestAttributes { Status = "sent" },
            id: "934b6a01"
        );

        // Act
        var json = JsonSerializer.Serialize(document, PingenJson.Options);

        // Assert
        json.Should().Be("""{"data":{"type":"letters","id":"934b6a01","attributes":{"status":"sent"}}}""");
    }

    private record TestResource
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("attributes")]
        public required TestAttributes Attributes { get; init; }

        [JsonPropertyName("relationships")]
        public TestRelationships? Relationships { get; init; }

        [JsonPropertyName("links")]
        public ResourceLinks? Links { get; init; }

        [JsonPropertyName("meta")]
        public ResourceMeta? Meta { get; init; }
    }

    private record TestAttributes
    {
        [JsonPropertyName("status")]
        public required string Status { get; init; }

        [JsonPropertyName("submitted_at")]
        public DateTimeOffset? SubmittedAt { get; init; }
    }

    private record TestRelationships
    {
        [JsonPropertyName("organisation")]
        public Relationship? Organisation { get; init; }

        [JsonPropertyName("events")]
        public RelatedCollection? Events { get; init; }
    }
}
