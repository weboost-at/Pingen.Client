using System.Net;
using System.Text;
using Pingen.Client.Files;

namespace Pingen.Client.Tests.Files.Support;

public abstract class FileTestBase : TestBase
{
    protected FileService Files => Resolve<FileService>();

    // A presigned URL is answered by the storage behind it, which refuses in S3's XML rather than a JSON:API error document
    protected HttpResponseMessage StorageRejection(HttpStatusCode status) =>
        new(status)
        {
            Content = new StringContent(
                "<Error><Code>AccessDenied</Code><Message>Request has expired</Message></Error>",
                Encoding.UTF8,
                "application/xml"
            )
        };
}
