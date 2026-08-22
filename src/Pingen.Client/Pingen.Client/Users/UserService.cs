using System.Runtime.CompilerServices;
using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Users;

/// <summary>The user the access token was issued for and the organisations it is associated with - these endpoints need the <c>user</c> scope, which the spec's scope list omits.</summary>
public class UserService(PingenClient client)
{
    private const string Path = "user";

    /// <summary>Fetches the authenticated user, a singleton resource addressed without an id.</summary>
    public async Task<User> GetAsync(CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<User>>(Path, cancellationToken)).Data;

    /// <summary>Lists one page of the memberships the user holds in organisations.</summary>
    public async Task<PingenList<Association>> ListAssociationsAsync(PingenListOptions? options = null, CancellationToken cancellationToken = default)
    {
        var document = await client.GetAsync<ListDocument<Association>>($"{Path}/associations", options, cancellationToken);

        return new(document.Data, document.Links, document.Meta);
    }

    /// <summary>Lists the memberships across page boundaries, fetching the next page as the enumeration reaches it.</summary>
    public async IAsyncEnumerable<Association> ListAssociationsAutoPagingAsync(
        PingenListOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var page = options ?? new();
        while (true)
        {
            var current = await ListAssociationsAsync(page, cancellationToken);
            if (current.Count is 0) yield break;

            foreach (var association in current) yield return association;

            if (current.Meta is not { } meta || meta.CurrentPage >= meta.LastPage) yield break;

            page = page with { PageNumber = meta.CurrentPage + 1 };
        }
    }
}
