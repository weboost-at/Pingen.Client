using System.Collections;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Common;

/// <summary>
/// One page of resources, enumerable on its own and carrying the pagination links and counters the API returned.
/// </summary>
public record PingenList<T>(IReadOnlyList<T> Data, ListLinks? Links, ListMeta? Meta) : IReadOnlyList<T>
{
    /// <summary>
    /// The item at <paramref name="index"/> on this page.
    /// </summary>
    public T this[int index] => Data[index];

    /// <summary>
    /// The number of items on this page.
    /// </summary>
    public int Count => Data.Count;

    /// <summary>
    /// Enumerates the items on this page.
    /// </summary>
    public IEnumerator<T> GetEnumerator() => Data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
