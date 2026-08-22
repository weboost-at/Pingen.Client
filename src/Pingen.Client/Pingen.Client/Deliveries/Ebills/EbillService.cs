using System.Runtime.CompilerServices;
using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;
using Pingen.Client.Files;

namespace Pingen.Client.Deliveries.Ebills;

/// <summary>The ebill channel - invoices are created, sent, cancelled and deleted here.</summary>
public class EbillService(PingenClient client)
{
    /// <summary>Lists one page of the organisation's ebills.</summary>
    public async Task<PingenList<Ebill>> ListAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default)
    {
        var document = await client.GetAsync<ListDocument<Ebill>>(Path(organisationId), options, cancellationToken);

        return new(document.Data, document.Links, document.Meta);
    }

    /// <summary>Lists the organisation's ebills across page boundaries, fetching the next page as the enumeration reaches it.</summary>
    public async IAsyncEnumerable<Ebill> ListAutoPagingAsync(
        Guid organisationId,
        PingenListOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var page = options ?? new();
        while (true)
        {
            var current = await ListAsync(organisationId, page, cancellationToken);
            if (current.Count is 0) yield break;

            foreach (var ebill in current) yield return ebill;

            if (current.Meta is not { } meta || meta.CurrentPage >= meta.LastPage) yield break;

            page = page with { PageNumber = meta.CurrentPage + 1 };
        }
    }

    /// <summary>Creates an ebill from a PDF already uploaded to a presigned URL.</summary>
    public async Task<Ebill> CreateAsync(Guid organisationId, EbillCreateOptions options, PingenRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) =>
        (await client.SendAsync<SingleDocument<Ebill>>(
            HttpMethod.Post,
            Path(organisationId),
            RequestDocument.For("ebills", options, presetId: options.PresetId),
            requestOptions,
            cancellationToken
        )).Data;

    /// <summary>Uploads <paramref name="content"/> to a presigned URL and creates an ebill from it - <see cref="EbillCreateOptions.FileUrl"/> and <see cref="EbillCreateOptions.FileUrlSignature"/> must be unset since the upload fills them.</summary>
    public async Task<Ebill> CreateAsync(
        Guid organisationId,
        Stream content,
        EbillCreateOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        if (options.FileUrl is not null || options.FileUrlSignature is not null)
            throw new ArgumentException("The upload overload fills FileUrl and FileUrlSignature from the upload it performs - leave both unset.", nameof(options));

        var upload = await new FileService(client).UploadAsync(content, cancellationToken);

        return await CreateAsync(
            organisationId,
            options with { FileUrl = upload.Attributes.Url, FileUrlSignature = upload.Attributes.UrlSignature },
            requestOptions,
            cancellationToken
        );
    }

    /// <summary>Fetches one ebill.</summary>
    public async Task<Ebill> GetAsync(Guid organisationId, Guid ebillId, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<Ebill>>(Path(organisationId, ebillId), cancellationToken)).Data;

    /// <summary>Deletes an ebill that has not been sent.</summary>
    public Task DeleteAsync(Guid organisationId, Guid ebillId, CancellationToken cancellationToken = default) =>
        client.SendAsync(HttpMethod.Delete, Path(organisationId, ebillId), null, null, cancellationToken);

    /// <summary>Cancels an ebill that is still cancellable.</summary>
    public Task CancelAsync(Guid organisationId, Guid ebillId, PingenRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) =>
        client.SendAsync(HttpMethod.Patch, $"{Path(organisationId, ebillId)}/cancel", null, requestOptions, cancellationToken);

    /// <summary>Dispatches an ebill that was created without auto-send - unlike letters, the ebill send endpoint takes no body.</summary>
    public async Task<Ebill> SendAsync(Guid organisationId, Guid ebillId, PingenRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) =>
        (await client.SendAsync<SingleDocument<Ebill>>(
            HttpMethod.Patch,
            $"{Path(organisationId, ebillId)}/send",
            null,
            requestOptions,
            cancellationToken
        )).Data;

    /// <summary>Reads the presigned URL of the ebill's PDF that the file endpoint answers its redirect with.</summary>
    public Task<Uri> GetFileLocationAsync(Guid organisationId, Guid ebillId, CancellationToken cancellationToken = default) =>
        client.GetLocationAsync($"{Path(organisationId, ebillId)}/file", cancellationToken);

    /// <summary>Downloads the ebill's PDF - the caller owns the stream and releases the connection by disposing it.</summary>
    public async Task<Stream> DownloadFileAsync(Guid organisationId, Guid ebillId, CancellationToken cancellationToken = default) =>
        await new FileService(client).DownloadAsync(await GetFileLocationAsync(organisationId, ebillId, cancellationToken), cancellationToken);

    /// <summary>Lists one page of the ebill's events.</summary>
    public async Task<PingenList<DeliverableEvent>> ListEventsAsync(Guid organisationId, Guid ebillId, PingenListOptions? options = null, CancellationToken cancellationToken = default)
    {
        var document = await client.GetAsync<ListDocument<DeliverableEvent>>($"{Path(organisationId, ebillId)}/events", options, cancellationToken);

        return new(document.Data, document.Links, document.Meta);
    }

    /// <summary>Reads the presigned URL of an event's image that the image endpoint answers its redirect with.</summary>
    public Task<Uri> GetEventImageLocationAsync(Guid organisationId, Guid ebillId, Guid eventId, CancellationToken cancellationToken = default) =>
        client.GetLocationAsync($"{Path(organisationId, ebillId)}/events/{eventId}/image", cancellationToken);

    /// <summary>Downloads an event's image - the caller owns the stream and releases the connection by disposing it.</summary>
    public async Task<Stream> DownloadEventImageAsync(Guid organisationId, Guid ebillId, Guid eventId, CancellationToken cancellationToken = default) =>
        await new FileService(client).DownloadAsync(await GetEventImageLocationAsync(organisationId, ebillId, eventId, cancellationToken), cancellationToken);

    private static string Path(Guid organisationId) => $"organisations/{organisationId}/deliveries/ebills";

    private static string Path(Guid organisationId, Guid ebillId) => $"{Path(organisationId)}/{ebillId}";
}
