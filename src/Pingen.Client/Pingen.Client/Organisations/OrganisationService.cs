using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Organisations;

/// <summary>The organisations the authenticated user is associated with.</summary>
public class OrganisationService(PingenClient client)
{
    private const string Path = "organisations";

    /// <summary>Lists one page of the organisations the user may act for.</summary>
    public async Task<PingenList<Organisation>> ListAsync(PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<Organisation>>(Path, options, cancellationToken)).ToList();

    /// <summary>Lists the organisations across page boundaries, fetching the next page as the enumeration reaches it.</summary>
    public IAsyncEnumerable<Organisation> ListAutoPagingAsync(PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        PingenPaging.EnumerateAsync(ListAsync, options, cancellationToken);

    /// <summary>Fetches a single organisation.</summary>
    public async Task<Organisation> GetAsync(Guid organisationId, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<Organisation>>($"{Path}/{organisationId}", cancellationToken)).Data;
}
