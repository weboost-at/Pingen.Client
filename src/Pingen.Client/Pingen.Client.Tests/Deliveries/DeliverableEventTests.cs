using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Common.JsonApi;
using Pingen.Client.Deliveries;

namespace Pingen.Client.Tests.Deliveries;

public class DeliverableEventTests
{
    [Fact]
    public void When_a_letter_event_arrives_Deserialize_maps_the_attributes_and_the_links()
    {
        // Act
        var document = JsonSerializer.Deserialize<ListDocument<DeliverableEvent>>(
            $$"""{ "data": [ {{Event("letters_events", "letter", "letters")}} ] }""",
            PingenJson.Options
        )!;

        // Assert
        var @event = document.Data.Should().ContainSingle().Which;
        @event.Id.Should().Be(Guid.Parse("934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e"));
        @event.Type.Should().Be("letters_events");
        @event.Attributes.Code.Should().Be("undeliverable");
        @event.Attributes.Name.Should().Be("Content failed inspection");
        @event.Attributes.Producer.Should().Be("Pingen");
        @event.Attributes.Location.Should().Be("8051 Zürich, CH");
        @event.Attributes.HasImage.Should().BeTrue();
        @event.Attributes.Data.Should().Equal("moved", "unknown");
        @event.Attributes.EmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        @event.Attributes.CreatedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        @event.Attributes.UpdatedAt.Should().Be(new DateTimeOffset(2021, 11, 20, 10, 0, 0, TimeSpan.FromHours(1)));
        @event.Links!.Self.Should().Be("https://api.pingen.com/organisations/6c3d1f0a/letters/2/events/934b6a01");
        @event.Meta.Should().BeNull();
    }

    [Theory]
    [InlineData("letters_events", "letter", "letters")]
    [InlineData("deliverables_events", "email", "emails")]
    [InlineData("deliverables_events", "ebill", "ebills")]
    public void When_an_event_names_its_parent_after_its_channel_Parent_returns_the_relationship_that_is_set(string type, string key, string parentType)
    {
        // Act
        var @event = JsonSerializer.Deserialize<DeliverableEvent>(Event(type, key, parentType), PingenJson.Options)!;

        // Assert
        var relationships = @event.Relationships!;
        relationships.Parent!.Data!.Id.Should().Be("6c3d1f0a-1111-4000-8000-000000000001");
        relationships.Parent.Data.Type.Should().Be(parentType);
        relationships.Parent.Links!.Related.Should().Be("https://api.pingen.com/deliverables/6c3d1f0a");
        new[] { relationships.Letter, relationships.Email, relationships.Ebill }
            .Where(channel => channel is not null)
            .Should().ContainSingle().Which.Should().Be(relationships.Parent);
    }

    private static string Event(string type, string key, string parentType) =>
        $$"""
          {
            "id": "934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e",
            "type": "{{type}}",
            "attributes": {
              "code": "undeliverable",
              "name": "Content failed inspection",
              "producer": "Pingen",
              "location": "8051 Zürich, CH",
              "has_image": true,
              "data": [ "moved", "unknown" ],
              "emitted_at": "2021-11-19T09:42:48+0100",
              "created_at": "2021-11-19T09:42:48+0100",
              "updated_at": "2021-11-20T10:00:00+0100"
            },
            "relationships": {
              "{{key}}": {
                "links": { "related": "https://api.pingen.com/deliverables/6c3d1f0a" },
                "data": { "id": "6c3d1f0a-1111-4000-8000-000000000001", "type": "{{parentType}}" }
              }
            },
            "links": { "self": "https://api.pingen.com/organisations/6c3d1f0a/letters/2/events/934b6a01" }
          }
          """;
}
