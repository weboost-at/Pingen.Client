using FluentAssertions;
using Pingen.Client.Webhooks.Payloads;

namespace Pingen.Client.Tests.Webhooks.Support;

public class WebhookEventAssertions(WebhookEvent @event)
{
    public T Is<T>() where T : WebhookEvent => @event.Should().BeOfType<T>().Subject;
}

public static class WebhookEventAssertionExtensions
{
    extension(WebhookEvent @event)
    {
        public WebhookEventAssertions Assert => new(@event);
    }
}
