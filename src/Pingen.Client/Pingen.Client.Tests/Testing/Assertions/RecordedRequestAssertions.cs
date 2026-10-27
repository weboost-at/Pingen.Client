using FluentAssertions;

namespace Pingen.Client.Tests.Testing.Assertions;

public class RecordedRequestAssertions(RecordedRequest request)
{
    public RecordedRequest Sent(HttpMethod method, string path)
    {
        request.Method.Should().Be(method);
        request.Path.Should().Be(path);
        return request;
    }

    // Unescaped, so the expectation reads the brackets and braces of Pingen's query grammar
    public RecordedRequest Query(string expected)
    {
        Uri.UnescapeDataString(request.Query).Should().Be(expected);
        return request;
    }

    public RecordedRequest Bodyless()
    {
        request.Body.Should().BeEmpty();
        return request;
    }

    public RecordedRequest Unauthenticated()
    {
        request.Header("Authorization").Should().BeNull();
        return request;
    }
}

public static class RecordedRequestAssertionExtensions
{
    extension(RecordedRequest request)
    {
        public RecordedRequestAssertions Assert => new(request);
    }
}
