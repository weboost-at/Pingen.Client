using System.Runtime.CompilerServices;
using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Batches;

/// <summary>The batches of an organisation - one upload split into many deliveries, dispatched through one channel.</summary>
public class BatchService(PingenClient client)
{
    private const string BatchType = "batches";

    /// <summary>Lists one page of the organisation's batches.</summary>
    public async Task<PingenList<Batch>> ListAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<Batch>>(BatchesPath(organisationId), options, cancellationToken)).ToList();

    /// <summary>Enumerates every batch of the organisation, fetching the next page whenever the enumeration runs off the current one.</summary>
    public async IAsyncEnumerable<Batch> ListAutoPagingAsync(
        Guid organisationId,
        PingenListOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var page = options ?? new();
        while (true)
        {
            var document = await client.GetAsync<ListDocument<Batch>>(BatchesPath(organisationId), page, cancellationToken);
            foreach (var batch in document.Data) yield return batch;

            if (document.Data.Count is 0 || document.Meta is not { } meta || meta.CurrentPage >= meta.LastPage) yield break;

            page = page with { PageNumber = meta.CurrentPage + 1 };
        }
    }

    /// <summary>Creates a batch from an archive or merged PDF that was already uploaded, which requires <see cref="BatchCreateOptions.FileUrl"/> and <see cref="BatchCreateOptions.FileUrlSignature"/> to be set.</summary>
    public async Task<Batch> CreateAsync(
        Guid organisationId,
        BatchCreateOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        var document = await client.SendAsync<SingleDocument<Batch>>(
            method: HttpMethod.Post,
            path: BatchesPath(organisationId),
            body: RequestDocument.For(BatchType, options, presetId: options.PresetId),
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

        return document.Data;
    }

    /// <summary>Uploads <paramref name="content"/> and creates a batch from it, filling <see cref="BatchCreateOptions.FileUrl"/> and <see cref="BatchCreateOptions.FileUrlSignature"/> from the upload - leave both unset.</summary>
    public async Task<Batch> CreateAsync(
        Guid organisationId,
        Stream content,
        BatchCreateOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        if (options.FileUrl is not null || options.FileUrlSignature is not null)
            throw new ArgumentException("This overload uploads the file itself and fills FileUrl and FileUrlSignature - leave both unset.", nameof(options));

        var upload = await client.Files.UploadAsync(content, cancellationToken);

        return await CreateAsync(
            organisationId,
            options with { FileUrl = upload.Attributes.Url, FileUrlSignature = upload.Attributes.UrlSignature },
            requestOptions,
            cancellationToken
        );
    }

    /// <summary>Fetches a single batch.</summary>
    public async Task<Batch> GetAsync(Guid organisationId, Guid batchId, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<Batch>>(BatchPath(organisationId, batchId), cancellationToken)).Data;

    /// <summary>Renames a batch or changes its icon, which Pingen accepts without answering with the batch.</summary>
    public Task EditAsync(
        Guid organisationId,
        Guid batchId,
        BatchEditOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    ) =>
        client.SendAsync(
            method: HttpMethod.Patch,
            path: BatchPath(organisationId, batchId),
            body: RequestDocument.For(BatchType, options, id: batchId.ToString()),
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

    /// <summary>Deletes a batch, taking its letters and deliveries with it as <paramref name="options"/> asks - this endpoint requires the body.</summary>
    // Pingen honours Idempotency-Key on POST and PATCH only, so no delete takes request options.
    public Task DeleteAsync(
        Guid organisationId,
        Guid batchId,
        BatchDeleteOptions options,
        CancellationToken cancellationToken = default
    ) =>
        client.SendAsync(
            method: HttpMethod.Delete,
            path: BatchPath(organisationId, batchId),
            body: RequestDocument.For(BatchType, options, id: batchId.ToString()),
            requestOptions: null,
            cancellationToken: cancellationToken
        );

    /// <summary>Cancels a batch that is already on its way through production, which Pingen accepts without answering with the batch.</summary>
    public Task CancelAsync(Guid organisationId, Guid batchId, PingenRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) =>
        client.SendAsync(
            method: HttpMethod.Patch,
            path: $"{BatchPath(organisationId, batchId)}/cancel",
            body: null,
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

    /// <summary>Sends a batch through the channel <paramref name="options"/> was built for.</summary>
    public async Task<Batch> SendAsync(
        Guid organisationId,
        Guid batchId,
        BatchSendOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        var document = await client.SendAsync<SingleDocument<Batch>>(
            method: HttpMethod.Patch,
            path: $"{BatchPath(organisationId, batchId)}/send",
            body: RequestDocument.For(options.Type, options, id: batchId.ToString()),
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

        return document.Data;
    }

    /// <summary>Lists one page of the events recorded on a batch.</summary>
    public async Task<PingenList<BatchEvent>> ListEventsAsync(
        Guid organisationId,
        Guid batchId,
        PingenListOptions? options = null,
        CancellationToken cancellationToken = default
    ) =>
        (await client.GetAsync<ListDocument<BatchEvent>>($"{BatchPath(organisationId, batchId)}/events", options, cancellationToken)).ToList();

    /// <summary>Fetches how the letters of a batch are distributed across validation groups, countries and regions.</summary>
    public async Task<BatchStatistics> GetStatisticsAsync(Guid organisationId, Guid batchId, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<BatchStatistics>>($"{BatchPath(organisationId, batchId)}/statistics", cancellationToken)).Data;

    private static string BatchesPath(Guid organisationId) => $"organisations/{organisationId}/batches";

    private static string BatchPath(Guid organisationId, Guid batchId) => $"{BatchesPath(organisationId)}/{batchId}";
}
