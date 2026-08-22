using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Users;

/// <summary>
/// The user the access token was issued for and the organisations it is associated with - these endpoints need the
/// <c>user</c> scope, which the spec's scope list omits.
/// </summary>
public class UserService(PingenClient client)
{
    private const string Path = "user";

    /// <summary>
    /// Fetches the authenticated user, a singleton resource addressed without an id.
    /// </summary>
    public async Task<User> GetAsync(CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<User>>(Path, cancellationToken)).Data;

    /// <summary>
    /// Lists one page of the memberships the user holds in organisations.
    /// </summary>
    public async Task<PingenList<Association>> ListAssociationsAsync(PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<Association>>($"{Path}/associations", options, cancellationToken)).ToList();

    /// <summary>
    /// Lists the memberships across page boundaries, fetching the next page as the enumeration reaches it.
    /// </summary>
    public IAsyncEnumerable<Association> ListAssociationsAutoPagingAsync(PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        PingenPaging.EnumerateAsync(ListAssociationsAsync, options, cancellationToken);
}
