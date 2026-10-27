using System.Globalization;

namespace Pingen.Client.Tests.Common.Support;

// Interpolated into error documents, so no value needs JSON escaping
public class ErrorTestData(TestFaker faker)
{
    private static readonly string[] Titles = ["The given data was invalid.", "Validation failed", "Wrong state", "Ungültige Anfrage"];

    private static readonly string[] Details =
    [
        "The file url signature is invalid.",
        "The meta data is incomplete",
        "Der Brief ist noch nicht gültig",
        "signing_key must be at least 20 characters"
    ];

    private static readonly string[] Pointers = ["/data/attributes/file_url", "/data/attributes/meta_data", "/data/id"];

    public string Code { get; } = faker.Int(1, 9999).ToString(CultureInfo.InvariantCulture);

    public string Title { get; } = faker.PickRandom(Titles);

    public string Detail { get; } = faker.PickRandom(Details);

    public string Pointer { get; } = faker.PickRandom(Pointers);
}
