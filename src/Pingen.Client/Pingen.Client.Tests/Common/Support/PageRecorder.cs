using Pingen.Client.Common;

namespace Pingen.Client.Tests.Common.Support;

// Stands in for a list endpoint: records every fetch and answers with the pages queued on it, in order
public class PageRecorder
{
    private readonly Queue<PingenList<string>> _pages = new();

    public List<PingenListOptions> Requests { get; } = [];

    public List<CancellationToken> Tokens { get; } = [];

    public PageRecorder Enqueue(PingenList<string> page)
    {
        _pages.Enqueue(page);

        return this;
    }

    public Task<PingenList<string>> FetchPageAsync(PingenListOptions options, CancellationToken cancellationToken)
    {
        Requests.Add(options);
        Tokens.Add(cancellationToken);

        if (_pages.Count is 0) throw new InvalidOperationException($"No page was queued for page {options.PageNumber}.");

        return Task.FromResult(_pages.Dequeue());
    }
}
