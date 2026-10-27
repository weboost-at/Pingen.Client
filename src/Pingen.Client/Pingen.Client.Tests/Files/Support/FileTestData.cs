namespace Pingen.Client.Tests.Files.Support;

public class FileTestData(TestFaker faker)
{
    public Guid UploadId { get; } = faker.Guid();

    public Uri Url { get; } = new($"https://s3.example.com/bucket/{faker.AlphaNumeric(12)}.pdf?X-Amz-Signature={faker.AlphaNumeric(32)}");

    public string Signature { get; } = $"$2y$10${faker.AlphaNumeric(43)}";

    // Where a file endpoint's 302 points, a presigned URL of its own
    public Uri Location { get; } = new($"https://s3.example.com/bucket/{faker.AlphaNumeric(12)}.pdf?X-Amz-Signature={faker.AlphaNumeric(32)}");

    public byte[] Pdf { get; } = [.. "%PDF-1.7 Zürich "u8, .. faker.Bytes(32)];
}
