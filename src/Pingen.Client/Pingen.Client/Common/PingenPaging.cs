using System.Runtime.CompilerServices;

namespace Pingen.Client.Common;

/// <summary>
/// The page walk every auto-paging enumeration of the library runs.
/// </summary>
public static class PingenPaging
{
    /// <summary>
    /// Enumerates a list endpoint across page boundaries, calling <paramref name="fetchPage"/> again whenever the
    /// enumeration reaches the end of the current page.
    /// </summary>
    public static async IAsyncEnumerable<T> EnumerateAsync<T>(
        Func<PingenListOptions, CancellationToken, Task<PingenList<T>>> fetchPage,
        PingenListOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var page = options ?? new();
        while (true)
        {
            var current = await fetchPage(page, cancellationToken);
            foreach (var item in current) yield return item;

            // A page without counters is the whole collection - the endpoint answered without pagination.
            if (current.Count is 0 || current.Meta is not { } meta || meta.CurrentPage >= meta.LastPage) yield break;

            page = page with { PageNumber = meta.CurrentPage + 1 };
        }
    }
}
