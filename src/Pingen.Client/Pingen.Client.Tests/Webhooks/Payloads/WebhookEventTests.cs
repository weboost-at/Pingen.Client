using System.Text.Json;
using FluentAssertions;
using Pingen.Client.Common.Json;
using Pingen.Client.Webhooks.Payloads;

namespace Pingen.Client.Tests.Webhooks.Payloads;

public class WebhookEventTests : UnitTestBase
{
    [Fact]
    public void Given_No_Envelope_Deserialize_Leaves_The_Type_Empty()
    {
        // Act
        var sent = JsonSerializer.Deserialize<WebhookSentEvent>(
            $$"""{ "url": "{{Data.Webhook.Url}}", "created_at": "2020-11-19T09:42:48+0100" }""",
            PingenJson.Options
        )!;

        // Assert
        sent.Type.Should().BeEmpty();
    }
}
