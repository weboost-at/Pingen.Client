using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Pingen.Client.Options;
using Pingen.Client.Tests.Common.Support;
using Pingen.Client.Tests.Files.Support;

namespace Pingen.Client.Tests.Testing.Bases;

public abstract class TestBase : UnitTestBase, IDisposable
{
    private PingenTestHost? _host;

    protected ErrorTestData Error => Data.Error;

    protected FileTestData File => Data.File;

    protected RequestTestData Request => Data.Request;

    protected PingenTestHost Host => _host ??= new();

    // Options only reach the container it builds, so a test configures its host before the first read of Host
    protected PingenTestHost HostWith(Action<PingenOptions> configure) => _host = new(configure);

    protected T Resolve<T>() where T : notnull => Host.Provider.GetRequiredService<T>();

    public void Dispose() => _host?.Dispose();

    protected string UploadDocument() =>
        $$"""
          {
            "data": {
              "id": "{{File.UploadId}}",
              "type": "file_uploads",
              "attributes": {
                "url": "{{File.Url}}",
                "url_signature": "{{File.Signature}}",
                "expires_at": "2021-11-19T09:42:48+0100"
              },
              "links": { "self": "https://api.pingen.com/file-upload" }
            }
          }
          """;

    protected HttpResponseMessage Rejection(HttpStatusCode status) =>
        new(status)
        {
            Headers = { { "X-Request-Id", Request.Id } },
            Content = new StringContent(
                $$"""
                  {
                    "errors": [
                      {
                        "code": "{{Error.Code}}",
                        "title": "{{Error.Title}}",
                        "detail": "{{Error.Detail}}",
                        "source": { "pointer": "{{Error.Pointer}}", "parameter": null }
                      }
                    ]
                  }
                  """,
                Encoding.UTF8,
                PingenClient.JsonApiMediaType
            )
        };
}
