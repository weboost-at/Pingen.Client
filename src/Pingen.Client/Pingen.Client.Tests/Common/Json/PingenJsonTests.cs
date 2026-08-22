using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Tests.Common.Json;

public class PingenJsonTests
{
    [Fact]
    public void When_a_member_is_null_Serialize_omits_it()
    {
        // Arrange
        var sample = new Sample
        {
            InvoiceDate = new(2021, 11, 19),
            CreatedAt = new(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)),
        };

        // Act
        var json = JsonSerializer.Serialize(sample, PingenJson.Options);

        // Assert
        json.Should().Be("""{"invoice_date":"2021-11-19","created_at":"2021-11-19T09:42:48+01:00"}""");
    }

    [Fact]
    public void When_the_response_carries_unknown_members_Deserialize_ignores_them()
    {
        // Arrange
        var json = """
                   {"name":"Zürich AG","invoice_date":"2021-11-19","submitted_at":"","created_at":"2021-11-19T09:42:48+0100","future_field":{"nested":[1,2]}}
                   """;

        // Act
        var sample = JsonSerializer.Deserialize<Sample>(json, PingenJson.Options)!;

        // Assert
        sample.Name.Should().Be("Zürich AG");
        sample.InvoiceDate.Should().Be(new DateOnly(2021, 11, 19));
        sample.SubmittedAt.Should().BeNull();
        sample.CreatedAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
    }

    private record Sample
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("invoice_date")]
        public DateOnly InvoiceDate { get; init; }

        [JsonPropertyName("submitted_at")]
        public DateTimeOffset? SubmittedAt { get; init; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
