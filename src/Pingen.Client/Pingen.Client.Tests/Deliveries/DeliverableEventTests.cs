using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Common.JsonApi;
using Pingen.Client.Deliveries;

namespace Pingen.Client.Tests.Deliveries;

public class DeliverableEventTests : UnitTestBase
{
    [Fact]
    public void Given_A_Letter_Event_Deserialize_Maps_The_Attributes_And_The_Links()
    {
        // Act
        var @event = JsonSerializer.Deserialize<DeliverableEvent>(Event("letters_events", "letter", "letters"), PingenJson.Options)!;

        // Assert
        @event.Meta.Should().BeNull();
        @event.Id.Should().Be(Data.Resource.Id);
        @event.Type.Should().Be("letters_events");
        @event.Links!.Self.Should().Be($"{ParentUrl("letters")}/events/{Data.Resource.Id}");

        @event.Attributes.Code.Should().Be("undeliverable");
        @event.Attributes.Name.Should().Be("Content failed inspection");
        @event.Attributes.Producer.Should().Be("Pingen");
        @event.Attributes.Location.Should().Be("8051 Zürich, CH");
        @event.Attributes.HasImage.Should().BeTrue();
        @event.Attributes.Data.Should().Equal("moved", "unknown");
        @event.Attributes.EmittedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        @event.Attributes.CreatedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
        @event.Attributes.UpdatedAt.Should().Be(new DateTimeOffset(2021, 11, 20, 10, 0, 0, TimeSpan.FromHours(1)));
    }

    [Theory]
    [InlineData("letters_events", "letter", "letters")]
    [InlineData("deliverables_events", "email", "emails")]
    [InlineData("deliverables_events", "ebill", "ebills")]
    public void Given_A_Channel_Relationship_Parent_Is_That_Relationship(string type, string key, string parentType)
    {
        // Act
        var @event = JsonSerializer.Deserialize<DeliverableEvent>(Event(type, key, parentType), PingenJson.Options)!;

        // Assert
        @event.Relationships!.Parent!.Data!.Type.Should().Be(parentType);
        @event.Relationships.Parent.Links!.Related.Should().Be(ParentUrl(parentType));
        @event.Relationships.Parent.Data.Id.Should().Be(Data.Resource.ParentId.ToString());

        new[] { @event.Relationships.Letter, @event.Relationships.Email, @event.Relationships.Ebill }
            .OfType<Relationship>()
            .Should()
            .Equal(@event.Relationships.Parent);
    }

    private string ParentUrl(string parentType) =>
        $"https://api.pingen.com/organisations/{Data.Organisation.Id}/deliveries/{parentType}/{Data.Resource.ParentId}";

    private string Event(string type, string key, string parentType) =>
        $$"""
          {
            "id": "{{Data.Resource.Id}}",
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
                "links": { "related": "{{ParentUrl(parentType)}}" },
                "data": { "id": "{{Data.Resource.ParentId}}", "type": "{{parentType}}" }
              }
            },
            "links": { "self": "{{ParentUrl(parentType)}}/events/{{Data.Resource.Id}}" }
          }
          """;
}
