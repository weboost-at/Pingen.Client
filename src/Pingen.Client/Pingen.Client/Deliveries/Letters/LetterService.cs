using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Deliveries.Letters;

/// <summary>
/// The letters of an organisation - creating, sending, cancelling and tracking physical mail.
/// </summary>
public class LetterService(PingenClient client)
{
    /// <summary>
    /// Lists one page of the organisation's letters.
    /// </summary>
    public async Task<PingenList<Letter>> ListAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<Letter>>(LettersPath(organisationId), options, cancellationToken)).ToList();

    /// <summary>
    /// Enumerates every letter of the organisation, fetching the next page whenever the enumeration runs off the
    /// current one.
    /// </summary>
    public IAsyncEnumerable<Letter> ListAutoPagingAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        PingenPaging.EnumerateAsync((page, token) => ListAsync(organisationId, page, token), options, cancellationToken);

    /// <summary>
    /// Creates a letter from a PDF that was already uploaded, which requires <see cref="LetterCreateOptions.FileUrl"/>
    /// and <see cref="LetterCreateOptions.FileUrlSignature"/> to be set.
    /// </summary>
    public async Task<Letter> CreateAsync(
        Guid organisationId,
        LetterCreateOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        var document = await client.SendAsync<SingleDocument<Letter>>(
            method: HttpMethod.Post,
            path: LettersPath(organisationId),
            body: RequestDocument.For(PingenType.Letters, options, presetId: options.PresetId),
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

        return document.Data;
    }

    /// <summary>
    /// Uploads <paramref name="content"/> and creates a letter from it, filling
    /// <see cref="LetterCreateOptions.FileUrl"/> and <see cref="LetterCreateOptions.FileUrlSignature"/> from the
    /// upload - leave both unset.
    /// </summary>
    public async Task<Letter> CreateAsync(
        Guid organisationId,
        Stream content,
        LetterCreateOptions options,
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

    /// <summary>
    /// Fetches a single letter.
    /// </summary>
    public async Task<Letter> GetAsync(Guid organisationId, Guid letterId, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<Letter>>(LetterPath(organisationId, letterId), cancellationToken)).Data;

    /// <summary>
    /// Deletes a letter that has not been sent yet.
    /// </summary>
    public Task DeleteAsync(Guid organisationId, Guid letterId, CancellationToken cancellationToken = default) =>
        client.SendAsync(
            method: HttpMethod.Delete,
            path: LetterPath(organisationId, letterId),
            body: null,
            requestOptions: null,
            cancellationToken: cancellationToken
        );

    /// <summary>
    /// Cancels a letter that is already on its way through production, which Pingen accepts without answering with the
    /// letter.
    /// </summary>
    public Task CancelAsync(Guid organisationId, Guid letterId, PingenRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) =>
        client.SendAsync(
            method: HttpMethod.Patch,
            path: $"{LetterPath(organisationId, letterId)}/cancel",
            body: null,
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

    /// <summary>
    /// Sends a letter that was created without auto-send.
    /// </summary>
    public async Task<Letter> SendAsync(
        Guid organisationId,
        Guid letterId,
        LetterSendOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        var document = await client.SendAsync<SingleDocument<Letter>>(
            method: HttpMethod.Patch,
            path: $"{LetterPath(organisationId, letterId)}/send",
            body: RequestDocument.For(PingenType.Letters, options, id: letterId.ToString()),
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

        return document.Data;
    }

    /// <summary>
    /// Resolves the presigned URL of the letter's PDF, which the API answers with instead of the file itself.
    /// </summary>
    public Task<Uri> GetFileLocationAsync(Guid organisationId, Guid letterId, CancellationToken cancellationToken = default) =>
        client.GetLocationAsync($"{LetterPath(organisationId, letterId)}/file", cancellationToken);

    /// <summary>
    /// Downloads the letter's PDF - the caller owns the returned stream.
    /// </summary>
    public async Task<Stream> DownloadFileAsync(Guid organisationId, Guid letterId, CancellationToken cancellationToken = default) =>
        await client.Files.DownloadAsync(await GetFileLocationAsync(organisationId, letterId, cancellationToken), cancellationToken);

    /// <summary>
    /// Lists one page of the events recorded on a letter.
    /// </summary>
    public async Task<PingenList<DeliverableEvent>> ListEventsAsync(
        Guid organisationId,
        Guid letterId,
        PingenListOptions? options = null,
        CancellationToken cancellationToken = default
    ) =>
        (await client.GetAsync<ListDocument<DeliverableEvent>>($"{LetterPath(organisationId, letterId)}/events", options, cancellationToken)).ToList();

    /// <summary>
    /// Resolves the presigned URL of the image an event carries, for example the scan of an undeliverable envelope.
    /// </summary>
    public Task<Uri> GetEventImageLocationAsync(Guid organisationId, Guid letterId, Guid eventId, CancellationToken cancellationToken = default) =>
        client.GetLocationAsync($"{LetterPath(organisationId, letterId)}/events/{eventId}/image", cancellationToken);

    /// <summary>
    /// Downloads the image an event carries - the caller owns the returned stream.
    /// </summary>
    public async Task<Stream> DownloadEventImageAsync(Guid organisationId, Guid letterId, Guid eventId, CancellationToken cancellationToken = default) =>
        await client.Files.DownloadAsync(await GetEventImageLocationAsync(organisationId, letterId, eventId, cancellationToken), cancellationToken);

    /// <summary>
    /// Lists one page of the sent events of every letter of the organisation - this endpoint does not sort, so
    /// <see cref="PingenListOptions.Sort"/> is ignored.
    /// </summary>
    public async Task<PingenList<DeliverableEvent>> ListSentEventsAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<DeliverableEvent>>($"{LettersPath(organisationId)}/events/sent", options.WithoutSort(), cancellationToken)).ToList();

    /// <summary>
    /// Lists one page of the delivered events of every letter of the organisation - this endpoint does not sort, so
    /// <see cref="PingenListOptions.Sort"/> is ignored.
    /// </summary>
    public async Task<PingenList<DeliverableEvent>> ListDeliveredEventsAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<DeliverableEvent>>($"{LettersPath(organisationId)}/events/delivered", options.WithoutSort(), cancellationToken)).ToList();

    /// <summary>
    /// Lists one page of the issue events of every letter of the organisation - this endpoint does not sort, so
    /// <see cref="PingenListOptions.Sort"/> is ignored.
    /// </summary>
    public async Task<PingenList<DeliverableEvent>> ListIssueEventsAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<DeliverableEvent>>($"{LettersPath(organisationId)}/events/issues", options.WithoutSort(), cancellationToken)).ToList();

    /// <summary>
    /// Lists one page of the undeliverable events of every letter of the organisation - this endpoint does not sort, so
    /// <see cref="PingenListOptions.Sort"/> is ignored.
    /// </summary>
    public async Task<PingenList<DeliverableEvent>> ListUndeliverableEventsAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<DeliverableEvent>>($"{LettersPath(organisationId)}/events/undeliverable", options.WithoutSort(), cancellationToken)).ToList();

    /// <summary>
    /// Calculates what a letter of the given shape would cost, returning <c>null</c> when Pingen accepts the request
    /// without a price because the calculation is still running.
    /// </summary>
    public async Task<LetterPrice?> CalculatePriceAsync(
        Guid organisationId,
        LetterPriceOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        // The documented 202 answer carries no body at all, which the request core maps to null - there is no price yet.
        var document = await client.SendOrDefaultAsync<SingleDocument<LetterPrice>>(
            method: HttpMethod.Post,
            path: $"{LettersPath(organisationId)}/price-calculator",
            body: RequestDocument.For(PingenType.LetterPriceCalculator, options),
            requestOptions: requestOptions,
            cancellationToken: cancellationToken
        );

        return document?.Data;
    }

    private static string LettersPath(Guid organisationId) => $"organisations/{organisationId}/deliveries/letters";

    private static string LetterPath(Guid organisationId, Guid letterId) => $"{LettersPath(organisationId)}/{letterId}";
}
