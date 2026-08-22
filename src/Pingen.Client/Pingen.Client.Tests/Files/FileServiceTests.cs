using System.Net;
using System.Text;
using FluentAssertions;
using Pingen.Client.Common;
using Pingen.Client.Files;
using Pingen.Client.Tests.Tests;

namespace Pingen.Client.Tests.Files;

public class FileServiceTests
{
    private const string UploadJson = """
                                      {
                                        "data": {
                                          "id": "934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e",
                                          "type": "file_uploads",
                                          "attributes": {
                                            "url": "https://s3.example.com/bucket/934b6a01.pdf?signer=url",
                                            "url_signature": "$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV",
                                            "expires_at": "2021-11-19T09:42:48+0100"
                                          },
                                          "links": { "self": "https://api.pingen.com/file-upload" }
                                        }
                                      }
                                      """;

    private static readonly byte[] Pdf = "%PDF-1.7 Zürich"u8.ToArray();

    [Fact]
    public async Task When_an_upload_target_is_requested_RequestUploadAsync_gets_file_upload_and_maps_the_attributes()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(UploadJson);

        // Act
        var upload = await new FileService(host.Client).RequestUploadAsync(TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Method.Should().Be(HttpMethod.Get);
        host.Api.Request.Path.Should().Be("/file-upload");
        host.Api.Request.Header("Authorization").Should().Be($"Bearer {PingenTestHost.AccessToken}");
        upload.Id.Should().Be(Guid.Parse("934b6a01-a0e6-4b03-8b9a-2a0b1d5b2c7e"));
        upload.Type.Should().Be("file_uploads");
        upload.Attributes.Url.Should().Be("https://s3.example.com/bucket/934b6a01.pdf?signer=url");
        upload.Attributes.UrlSignature.Should().Be("$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV");
        upload.Attributes.ExpiresAt.Should().Be(new DateTimeOffset(2021, 11, 19, 9, 42, 48, TimeSpan.FromHours(1)));
    }

    [Fact]
    public async Task When_content_is_uploaded_UploadAsync_puts_the_raw_bytes_to_the_presigned_url_without_authentication()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(UploadJson);
        host.Files.EnqueueEmpty(HttpStatusCode.OK);
        using var content = new MemoryStream(Pdf);

        // Act
        var upload = await new FileService(host.Client).UploadAsync(content, TestContext.Current.CancellationToken);

        // Assert
        host.Api.Request.Path.Should().Be("/file-upload");
        host.Files.Request.Method.Should().Be(HttpMethod.Put);
        host.Files.Request.Url.Should().Be(new Uri("https://s3.example.com/bucket/934b6a01.pdf?signer=url"));
        host.Files.Request.Body.Should().Equal(Pdf);
        host.Files.Request.Header("Content-Type").Should().BeNull();
        host.Files.Request.Header("Authorization").Should().BeNull();
        upload.Attributes.UrlSignature.Should().Be("$2y$10$BLOzVbYTXrh4LZbSYNVf7eEDrc58vvQ9PRVZABqV");
    }

    [Fact]
    public async Task When_the_content_cannot_seek_UploadAsync_still_declares_a_content_length()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(UploadJson);
        host.Files.EnqueueEmpty(HttpStatusCode.OK);
        await using var content = new UnseekableStream(Pdf);

        // Act
        await new FileService(host.Client).UploadAsync(content, TestContext.Current.CancellationToken);

        // Assert
        host.Files.Request.Method.Should().Be(HttpMethod.Put);
        host.Files.Request.Body.Should().Equal(Pdf);
        host.Files.Request.Header("Content-Length").Should().Be(Pdf.Length.ToString());
    }

    [Fact]
    public async Task When_the_presigned_url_rejects_the_upload_UploadAsync_throws_with_the_status()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Api.EnqueueOk(UploadJson);
        host.Files.Enqueue(new(HttpStatusCode.Forbidden) { Content = new StringContent("<Error>expired</Error>", Encoding.UTF8, "application/xml") });
        using var content = new MemoryStream(Pdf);

        // Act
        var act = () => new FileService(host.Client).UploadAsync(content, TestContext.Current.CancellationToken);

        // Assert
        var exception = (await act.Should().ThrowAsync<PingenException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task When_a_presigned_url_is_given_DownloadAsync_streams_it_without_authentication()
    {
        // Arrange
        using var host = new PingenTestHost();
        host.Files.Enqueue(new(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) });

        // Act
        await using var file = await new FileService(host.Client).DownloadAsync(
            new("https://s3.example.com/bucket/934b6a01.pdf?signer=url"),
            TestContext.Current.CancellationToken
        );

        // Assert
        using var downloaded = new MemoryStream();
        await file.CopyToAsync(downloaded, TestContext.Current.CancellationToken);
        downloaded.ToArray().Should().Equal(Pdf);
        host.Files.Request.Method.Should().Be(HttpMethod.Get);
        host.Files.Request.Header("Authorization").Should().BeNull();
        host.Api.Requests.Should().BeEmpty();
    }

    // A PDF arriving from a network stream or a decompressor cannot seek - StreamContent has no length to declare for it and the upload goes out chunked.
    private class UnseekableStream(byte[] content) : Stream
    {
        private readonly MemoryStream _content = new(content);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => _content.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
