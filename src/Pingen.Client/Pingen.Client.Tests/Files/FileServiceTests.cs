using System.Net;
using FluentAssertions;
using Pingen.Client.Tests.Files.Support;

namespace Pingen.Client.Tests.Files;

public class FileServiceTests : FileTestBase
{
    [Fact]
    public async Task RequestUploadAsync_Gets_File_Upload_And_Maps_The_Attributes()
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
    public async Task UploadAsync_Puts_The_Raw_Bytes_To_The_Presigned_Url_Unauthenticated()
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
    public async Task Given_Unseekable_Content_UploadAsync_Still_Declares_A_Content_Length()
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
    public async Task Given_A_Rejected_Upload_UploadAsync_Throws_With_The_Status()
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
    public async Task DownloadAsync_Streams_The_Presigned_Url_Without_Authentication()
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

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Given_A_Refused_Download_DownloadAsync_Throws_With_The_Status(HttpStatusCode status)
    {
        // Arrange
        Host.Files.Enqueue(StorageRejection(status));

        // Act
        var act = () => Files.DownloadAsync(File.Location, Ct);

        // Assert
        var exception = await act.Assert.ThrowsAsync(status);
        exception.Errors.Should().BeEmpty();
    }
}
