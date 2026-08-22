using System.Runtime.CompilerServices;
using Pingen.Client.Common;
using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Deliveries.Emails;

/// <summary>The email channel - emails are created, cancelled and deleted here, and dispatch happens automatically or through a batch since the channel has no send endpoint.</summary>
public class EmailService(PingenClient client)
{
    /// <summary>Lists one page of the organisation's emails.</summary>
    public async Task<PingenList<Email>> ListAsync(Guid organisationId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<Email>>(Path(organisationId), options, cancellationToken)).ToList();

    /// <summary>Lists the organisation's emails across page boundaries, fetching the next page as the enumeration reaches it.</summary>
    public async IAsyncEnumerable<Email> ListAutoPagingAsync(
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

            foreach (var email in current) yield return email;

            if (current.Meta is not { } meta || meta.CurrentPage >= meta.LastPage) yield break;

            page = page with { PageNumber = meta.CurrentPage + 1 };
        }
    }

    /// <summary>Creates an email from a PDF already uploaded to a presigned URL.</summary>
    public async Task<Email> CreateAsync(Guid organisationId, EmailCreateOptions options, PingenRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) =>
        (await client.SendAsync<SingleDocument<Email>>(
            HttpMethod.Post,
            Path(organisationId),
            RequestDocument.For("emails", options, presetId: options.PresetId),
            requestOptions,
            cancellationToken
        )).Data;

    /// <summary>Uploads <paramref name="content"/> to a presigned URL and creates an email from it - <see cref="EmailCreateOptions.FileUrl"/> and <see cref="EmailCreateOptions.FileUrlSignature"/> must be unset since the upload fills them.</summary>
    public async Task<Email> CreateAsync(
        Guid organisationId,
        Stream content,
        EmailCreateOptions options,
        PingenRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default
    )
    {
        if (options.FileUrl is not null || options.FileUrlSignature is not null)
            throw new ArgumentException("The upload overload fills FileUrl and FileUrlSignature from the upload it performs - leave both unset.", nameof(options));

        var upload = await client.Files.UploadAsync(content, cancellationToken);

        return await CreateAsync(
            organisationId,
            options with { FileUrl = upload.Attributes.Url, FileUrlSignature = upload.Attributes.UrlSignature },
            requestOptions,
            cancellationToken
        );
    }

    /// <summary>Fetches one email.</summary>
    public async Task<Email> GetAsync(Guid organisationId, Guid emailId, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<Email>>(Path(organisationId, emailId), cancellationToken)).Data;

    /// <summary>Deletes an email that has not been sent.</summary>
    public Task DeleteAsync(Guid organisationId, Guid emailId, CancellationToken cancellationToken = default) =>
        client.SendAsync(HttpMethod.Delete, Path(organisationId, emailId), null, null, cancellationToken);

    /// <summary>Cancels an email that is still cancellable.</summary>
    public Task CancelAsync(Guid organisationId, Guid emailId, PingenRequestOptions? requestOptions = null, CancellationToken cancellationToken = default) =>
        client.SendAsync(HttpMethod.Patch, $"{Path(organisationId, emailId)}/cancel", null, requestOptions, cancellationToken);

    /// <summary>Reads the presigned URL of the email's PDF that the file endpoint answers its redirect with.</summary>
    public Task<Uri> GetFileLocationAsync(Guid organisationId, Guid emailId, CancellationToken cancellationToken = default) =>
        client.GetLocationAsync($"{Path(organisationId, emailId)}/file", cancellationToken);

    /// <summary>Downloads the email's PDF - the caller owns the stream and releases the connection by disposing it.</summary>
    public async Task<Stream> DownloadFileAsync(Guid organisationId, Guid emailId, CancellationToken cancellationToken = default) =>
        await client.Files.DownloadAsync(await GetFileLocationAsync(organisationId, emailId, cancellationToken), cancellationToken);

    /// <summary>Lists one page of the email's events.</summary>
    public async Task<PingenList<DeliverableEvent>> ListEventsAsync(Guid organisationId, Guid emailId, PingenListOptions? options = null, CancellationToken cancellationToken = default) =>
        (await client.GetAsync<ListDocument<DeliverableEvent>>($"{Path(organisationId, emailId)}/events", options, cancellationToken)).ToList();

    /// <summary>Reads the presigned URL of an event's image that the image endpoint answers its redirect with.</summary>
    public Task<Uri> GetEventImageLocationAsync(Guid organisationId, Guid emailId, Guid eventId, CancellationToken cancellationToken = default) =>
        client.GetLocationAsync($"{Path(organisationId, emailId)}/events/{eventId}/image", cancellationToken);

    /// <summary>Downloads an event's image - the caller owns the stream and releases the connection by disposing it.</summary>
    public async Task<Stream> DownloadEventImageAsync(Guid organisationId, Guid emailId, Guid eventId, CancellationToken cancellationToken = default) =>
        await client.Files.DownloadAsync(await GetEventImageLocationAsync(organisationId, emailId, eventId, cancellationToken), cancellationToken);

    private static string Path(Guid organisationId) => $"organisations/{organisationId}/deliveries/emails";

    private static string Path(Guid organisationId, Guid emailId) => $"{Path(organisationId)}/{emailId}";
}
