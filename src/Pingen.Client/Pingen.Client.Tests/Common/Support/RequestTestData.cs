namespace Pingen.Client.Tests.Common.Support;

public class RequestTestData(TestFaker faker)
{
    public string IdempotencyKey { get; } = faker.Guid().ToString();

    // What a failed response names in X-Request-Id
    public string Id { get; } = faker.AlphaNumeric(26).ToUpperInvariant();

    public TimeSpan RetryAfter { get; } = TimeSpan.FromSeconds(faker.Int(1, 120));
}
