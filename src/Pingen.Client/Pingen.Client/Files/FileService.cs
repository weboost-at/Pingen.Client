using Pingen.Client.Common.JsonApi;

namespace Pingen.Client.Files;

/// <summary>The file transfer half of the API - presigned upload targets, raw uploads and downloads of the URLs the file endpoints redirect to.</summary>
public class FileService(PingenClient client)
{
    /// <summary>Requests a presigned, single-use upload target.</summary>
    public async Task<FileUpload> RequestUploadAsync(CancellationToken cancellationToken = default) =>
        (await client.GetAsync<SingleDocument<FileUpload>>("file-upload", cancellationToken)).Data;

    /// <summary>Writes <paramref name="content"/> to a presigned upload target as raw bytes - never multipart, never authenticated, since a bearer token invalidates the presigned signature.</summary>
    public async Task UploadAsync(FileUpload target, Stream content, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, target.Attributes.Url) { Content = await MeasuredAsync(content, cancellationToken) };
        using var response = await client.FileClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await PingenClient.ToExceptionAsync(response, cancellationToken);
    }

    /// <summary>Requests an upload target and writes <paramref name="content"/> to it, returning the target whose URL and signature the create call carries.</summary>
    public async Task<FileUpload> UploadAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var upload = await RequestUploadAsync(cancellationToken);
        await UploadAsync(upload, content, cancellationToken);

        return upload;
    }

    /// <summary>Fetches the file behind a presigned URL, for example the <c>Location</c> a file endpoint answered its 302 with.</summary>
    public async Task<Stream> DownloadAsync(Uri location, CancellationToken cancellationToken = default)
    {
        var response = await client.FileClient.GetAsync(location, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // The response outlives this call - the caller owns the stream and releases the connection by disposing it.
        if (response.IsSuccessStatusCode) return await response.Content.ReadAsStreamAsync(cancellationToken);

        using (response) throw await PingenClient.ToExceptionAsync(response, cancellationToken);
    }

    // A presigned target answers 501 to a chunked upload, and a stream that cannot report its length goes out chunked - buffer it so Content-Length is known.
    private static async Task<HttpContent> MeasuredAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.CanSeek) return new StreamContent(content);

        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        return new ByteArrayContent(buffer.GetBuffer(), 0, (int)buffer.Length);
    }
}
