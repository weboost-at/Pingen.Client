using System.Net;
using FluentAssertions;
using Pingen.Client.Tests.Files.Support;

namespace Pingen.Client.Tests.Files;

public class FileServiceTests : FileTestBase
{
    [Fact]
    public async Task When_an_upload_target_is_requested_RequestUploadAsync_gets_file_upload_and_maps_the_attributes()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument());

        // Act
        var upload = await Files.RequestUploadAsync(Ct);

        // Assert
        upload.Id.Should().Be(File.UploadId);
        upload.Type.Should().Be("file_uploads");
        upload.Attributes.Url.Should().Be(File.Url.ToString());
        upload.Attributes.UrlSignature.Should().Be(File.Signature);
        upload.Attributes.ExpiresAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));

        Host.Api.Request.Assert.Sent(HttpMethod.Get, "/file-upload");
        Host.Api.Request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");
    }

    [Fact]
    public async Task When_content_is_uploaded_UploadAsync_puts_the_raw_bytes_to_the_presigned_url_unauthenticated()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument());
        Host.Files.EnqueueEmpty(HttpStatusCode.OK);

        using var content = new MemoryStream(File.Pdf);

        // Act
        var upload = await Files.UploadAsync(content, Ct);

        // Assert
        upload.Attributes.UrlSignature.Should().Be(File.Signature);

        Host.Api.Request.Path.Should().Be("/file-upload");

        Host.Files.Request.Body.Should().Equal(File.Pdf);
        Host.Files.Request.Url.Should().Be(File.Url);
        Host.Files.Request.Method.Should().Be(HttpMethod.Put);
        Host.Files.Request.Assert.Unauthenticated();
        Host.Files.Request.Header("Content-Type").Should().BeNull();
    }

    [Fact]
    public async Task When_the_content_cannot_seek_UploadAsync_still_declares_a_content_length()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument());
        Host.Files.EnqueueEmpty(HttpStatusCode.OK);

        await using var content = new UnseekableStream(File.Pdf);

        // Act
        await Files.UploadAsync(content, Ct);

        // Assert
        Host.Files.Request.Body.Should().Equal(File.Pdf);
        Host.Files.Request.Method.Should().Be(HttpMethod.Put);
        Host.Files.Request.Header("Content-Length").Should().Be(File.Pdf.Length.ToString());
    }

    [Fact]
    public async Task When_the_presigned_url_rejects_the_upload_UploadAsync_throws_with_the_status()
    {
        // Arrange
        Host.Api.EnqueueOk(UploadDocument());
        Host.Files.Enqueue(StorageRejection(HttpStatusCode.Forbidden));

        using var content = new MemoryStream(File.Pdf);

        // Act
        var act = () => Files.UploadAsync(content, Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(HttpStatusCode.Forbidden);
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_presigned_url_is_given_DownloadAsync_streams_it_without_authentication()
    {
        // Arrange
        Host.Files.EnqueueBytes(File.Pdf);

        using var downloaded = new MemoryStream();

        // Act
        await using var file = await Files.DownloadAsync(File.Location, Ct);
        await file.CopyToAsync(downloaded, Ct);

        // Assert
        downloaded.ToArray().Should().Equal(File.Pdf);

        Host.Api.Requests.Should().BeEmpty();

        Host.Files.Request.Url.Should().Be(File.Location);
        Host.Files.Request.Method.Should().Be(HttpMethod.Get);
        Host.Files.Request.Assert.Unauthenticated();
    }
}
