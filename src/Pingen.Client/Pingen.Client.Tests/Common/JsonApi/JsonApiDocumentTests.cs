using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Tests.Common.JsonApi;

public class JsonApiDocumentTests : UnitTestBase
{
    private string OrganisationUrl => $"https://api.pingen.com/organisations/{Data.Organisation.Id}";

    private string ResourceUrl => $"{OrganisationUrl}/deliveries/letters/{Data.Resource.Id}";

    [Fact]
    public void Given_A_Single_Document_Deserialize_Maps_Links_Relationships_And_Abilities()
    {
        // Act
        var document = JsonSerializer.Deserialize<SingleDocument<TestResource>>(ResourceDocument(), PingenJson.Options)!;

        // Assert
        document.Data.Attributes.Status.Should().Be("sent");
        document.Data.Attributes.SubmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));

        document.Data.Links!.Self.Should().Be(ResourceUrl);

        document.Data.Relationships!.Events!.Count.Should().Be(3);
        document.Data.Relationships.Events.Href.Should().Be($"{ResourceUrl}/events");
        document.Data.Relationships.Organisation!.Data!.Type.Should().Be("organisations");
        document.Data.Relationships.Organisation.Links!.Related.Should().Be(OrganisationUrl);
        document.Data.Relationships.Organisation.Data.Id.Should().Be(Data.Organisation.Id.ToString());

        document.Data.Meta!.Abilities.Should().Equal(new Dictionary<string, string>
        {
            ["cancel"] = "ok",
            ["send"] = "state",
            ["delete"] = "permission"
        });

        document.Included!.Should().ContainSingle();
        document.Included[0].Assert.Has("type", "organisations");
    }

    [Fact]
    public void Given_A_List_Document_Deserialize_Maps_The_Page_And_Leaves_Item_Meta_Null()
    {
        // Act
        var document = JsonSerializer.Deserialize<ListDocument<TestResource>>(ResourcePage, PingenJson.Options)!;

        // Assert
        document.Data.Should().HaveCount(2);
        document.Data[0].Meta.Should().BeNull();
        document.Data[1].Attributes.SubmittedAt.Should().BeNull();

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
    public void Given_A_Create_Document_Serialize_Writes_The_Envelope_Without_An_Id()
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
    public void Given_A_Send_Document_Serialize_Repeats_The_Path_Id_In_The_Body()
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

    private string ResourceDocument() =>
        $$"""
          {
            "data": {
              "id": "{{Data.Resource.Id}}",
              "type": "letters",
              "attributes": { "status": "sent", "submitted_at": "2021-11-19T09:42:48+0100" },
              "relationships": {
                "organisation": {
                  "links": { "related": "{{OrganisationUrl}}" },
                  "data": { "id": "{{Data.Organisation.Id}}", "type": "organisations" }
                },
                "events": {
                  "links": { "related": { "href": "{{ResourceUrl}}/events", "meta": { "count": 3 } } }
                }
              },
              "links": { "self": "{{ResourceUrl}}" },
              "meta": { "abilities": { "self": { "cancel": "ok", "send": "state", "delete": "permission" } } }
            },
            "included": [ { "id": "{{Data.Organisation.Id}}", "type": "organisations", "attributes": { "name": "Pingen AG" } } ]
          }
          """;

    private const string ResourcePage =
        """
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
