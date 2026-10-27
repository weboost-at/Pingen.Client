using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Pingen.Client.Tests.Testing.Hosting;

// Records every request it receives and answers with the responses queued on it, in order
public class RecordingHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public RecordedRequest Request => Requests.Single();

    // Awaited after a request is recorded and before it is answered, for tests that act while a call is in flight
    public Func<RecordedRequest, Task>? OnRequest { get; set; }

    public RecordingHandler Enqueue(HttpResponseMessage response)
    {
        _responses.Enqueue(response);

        return this;
    }

    // Drops what is queued and recorded, for tests that answer the first request themselves
    public RecordingHandler Clear()
    {
        _responses.Clear();
        Requests.Clear();

        return this;
    }

    public RecordingHandler EnqueueJson(HttpStatusCode status, string json, string mediaType = PingenClient.JsonApiMediaType) =>
        Enqueue(new(status) { Content = new StringContent(json, Encoding.UTF8, mediaType) });

    public RecordingHandler EnqueueOk(string json) => EnqueueJson(HttpStatusCode.OK, json);

    public RecordingHandler EnqueueEmpty(HttpStatusCode status = HttpStatusCode.NoContent) => Enqueue(new(status));

    public RecordingHandler EnqueueRedirect(Uri location) => Enqueue(new(HttpStatusCode.Found) { Headers = { Location = location } });

    public RecordingHandler EnqueueBytes(byte[] content) => Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent(content) });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, values) in request.Headers) headers[name] = string.Join(", ", values);
        if (request.Content is { } content)
        {
            // Content-Length is computed on first read and only then joins the header collection - a content that cannot report one goes out chunked.
            if (content.Headers.ContentLength is { } length) headers["Content-Length"] = length.ToString(CultureInfo.InvariantCulture);

            foreach (var (name, values) in content.Headers) headers[name] = string.Join(", ", values);
        }

        byte[] body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var recorded = new RecordedRequest(
            Method: request.Method,
            Url: request.RequestUri!,
            Version: request.Version,
            Headers: headers,
            Body: body
        );
        Requests.Add(recorded);

        if (OnRequest is { } hook) await hook(recorded);

        if (_responses.Count is 0) throw new InvalidOperationException($"No response was queued for {request.Method} {request.RequestUri}.");

        return _responses.Dequeue();
    }
}

public record RecordedRequest(
    HttpMethod Method,
    Uri Url,
    Version Version,
    IReadOnlyDictionary<string, string> Headers,
    byte[] Body
)
{
    public string Path => Url.AbsolutePath;

    // Still escaped, and with its leading ?
    public string Query => Url.Query;

    public string Text => Encoding.UTF8.GetString(Body);

    public JsonElement Json => JsonDocument.Parse(Body).RootElement;

    public JsonElement Data => Json.GetProperty("data");

    public JsonElement Attributes => Data.GetProperty("attributes");

    public string? Header(string name) => Headers.GetValueOrDefault(name);
}
