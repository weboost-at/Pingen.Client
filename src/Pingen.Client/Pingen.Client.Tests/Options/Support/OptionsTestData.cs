namespace Pingen.Client.Tests.Options.Support;

public class OptionsTestData(TestFaker faker)
{
    private static readonly string[] ScopeLists = ["letter", "letter webhook user", "batch organisation_read", "letter ebill email"];

    public string ClientId { get; } = faker.AlphaNumeric(32);

    public string ClientSecret { get; } = faker.AlphaNumeric(48);

    public string Scopes { get; } = faker.PickRandom(ScopeLists);

    public Uri BaseAddress { get; } = new($"https://api.{faker.AlphaNumeric(8)}.example.com/");

    public Uri IdentityAddress { get; } = new($"https://identity.{faker.AlphaNumeric(8)}.example.com/");
}
