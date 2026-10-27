namespace Pingen.Client.Tests.Authentication.Support;

// Interpolated into token documents, so no value needs JSON escaping
public class AuthenticationTestData(TestFaker faker)
{
    public string ExpiringToken { get; } = faker.AlphaNumeric(40);

    public string FreshToken { get; } = faker.AlphaNumeric(40);
}
