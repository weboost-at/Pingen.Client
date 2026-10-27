using System.Net;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Webhooks.Payloads;

namespace Pingen.Client.Tests.Webhooks.Support;

public class WebhookEventFunctionAssertions(Func<WebhookEvent> act)
{
    public PingenError ThrowsRejection()
    {
        var thrown = act.Should().Throw<PingenException>();
        thrown.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        thrown.Which.Errors.Should().ContainSingle();
        thrown.Which.Errors[0].Title.Should().Be("The webhook payload was rejected");
        return thrown.Which.Errors[0];
    }
}

public static class WebhookEventFunctionAssertionExtensions
{
    extension(Func<WebhookEvent> act)
    {
        public WebhookEventFunctionAssertions Assert => new(act);
    }
}
