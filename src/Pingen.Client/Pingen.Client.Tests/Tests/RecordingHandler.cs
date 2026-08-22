using System.Net;
using System.Text;
using System.Text.Json;

namespace Pingen.Client.Tests.Tests;

/// <summary>An HTTP handler that records every request it receives and answers with the responses queued on it.</summary>
public class RecordingHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    /// <summary>The requests that reached this handler, in the order they were sent.</summary>
    public List<RecordedRequest> Requests { get; } = [];

    /// <summary>The only request that reached this handler.</summary>
    public RecordedRequest Request => Requests.Single();

    /// <summary>Queues <paramref name="response"/> as the answer to the next request.</summary>
    public RecordingHandler Enqueue(HttpResponseMessage response)
    {
        _responses.Enqueue(response);

        return this;
    }

    /// <summary>Drops every queued response and recorded request, for tests that need to answer the first request themselves.</summary>
    public RecordingHandler Clear()
    {
        _responses.Clear();
        Requests.Clear();

        return this;
    }

    /// <summary>Queues a JSON:API answer with the given status and body.</summary>
    public RecordingHandler EnqueueJson(HttpStatusCode status, string json, string mediaType = PingenClient.JsonApiMediaType) =>
        Enqueue(new(status) { Content = new StringContent(json, Encoding.UTF8, mediaType) });

    /// <summary>Queues a <c>200 OK</c> JSON:API answer.</summary>
    public RecordingHandler EnqueueOk(string json) => EnqueueJson(HttpStatusCode.OK, json);

    /// <summary>Queues an answer without a body, for the 202 and 204 endpoints.</summary>
    public RecordingHandler EnqueueEmpty(HttpStatusCode status = HttpStatusCode.NoContent) => Enqueue(new(status));

    /// <summary>Records the request and answers it with the response queued first.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, values) in request.Headers) headers[name] = string.Join(", ", values);
        if (request.Content is { } content)
            foreach (var (name, values) in content.Headers)
                headers[name] = string.Join(", ", values);

        byte[] body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        Requests.Add(new(
            Method: request.Method,
            Url: request.RequestUri!,
            Headers: headers,
            Body: body
        ));

        if (_responses.Count is 0) throw new InvalidOperationException($"No response was queued for {request.Method} {request.RequestUri}.");

        return _responses.Dequeue();
    }
}

/// <summary>One request a <see cref="RecordingHandler"/> captured.</summary>
public record RecordedRequest(HttpMethod Method, Uri Url, IReadOnlyDictionary<string, string> Headers, byte[] Body)
{
    /// <summary>The path of the request without the query string.</summary>
    public string Path => Url.AbsolutePath;

    /// <summary>The query string of the request as it went on the wire, including the leading <c>?</c>.</summary>
    public string Query => Url.Query;

    /// <summary>The body of the request decoded as UTF-8 text.</summary>
    public string Text => Encoding.UTF8.GetString(Body);

    /// <summary>The body of the request parsed as JSON.</summary>
    public JsonElement Json => JsonDocument.Parse(Body).RootElement;

    /// <summary>The value of the header with the given name, null when the request carried none.</summary>
    public string? Header(string name) => Headers.GetValueOrDefault(name);
}
