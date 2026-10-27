using System.Net;
using FluentAssertions;
using Pingen.Client.Common;

namespace Pingen.Client.Tests.Testing.Assertions;

public class AsyncFunctionAssertions(Func<Task> act)
{
    public async Task<PingenException> ThrowsAsync(HttpStatusCode status)
    {
        var thrown = await act.Should().ThrowAsync<PingenException>();
        thrown.Which.StatusCode.Should().Be(status);
        return thrown.Which;
    }
}

public static class AsyncFunctionAssertionExtensions
{
    extension(Func<Task> act)
    {
        public AsyncFunctionAssertions Assert => new(act);
    }

    extension<T>(Func<Task<T>> act)
    {
        public AsyncFunctionAssertions Assert => new(act);
    }
}
