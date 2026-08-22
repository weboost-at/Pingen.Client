using System.Runtime.CompilerServices;
using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Organisations;

/// <summary>The organisations the authenticated user is associated with.</summary>
public class OrganisationService(PingenClient client)
{
    private const string Path = "organisations";

    /// <summary>Lists one page of the organisations the user may act for.</summary>
    public async Task<PingenList<Organisation>> ListAsync(PingenListOptions? options = null, CancellationToken cancellationToken = default)
    {
        var document = await client.GetAsync<ListDocument<Organisation>>(Path, options, cancellationToken);

        return new(document.Data, document.Links, document.Meta);
    }

    /// <summary>Lists the organisations across page boundaries, fetching the next page as the enumeration reaches it.</summary>
    public async IAsyncEnumerable<Organisation> ListAutoPagingAsync(
        PingenListOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var page = options ?? new();
        while (true)
        {
            var current = await ListAsync(page, cancellationToken);
            if (current.Count is 0) yield break;

            foreach (var organisation in current) yield return organisation;

            if (current.Meta is not { } meta || meta.CurrentPage >= meta.LastPage) yield break;

            page = page with { PageNumber = meta.CurrentPage + 1 };
        }
    }

    /// <summary>Fetches a single organisation.</summary>
    public async Task<Organisation> GetAsync(Guid organisationId, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<Organisation>>($"{Path}/{organisationId}", cancellationToken)).Data;
}
