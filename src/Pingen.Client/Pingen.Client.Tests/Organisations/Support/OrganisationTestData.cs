namespace Pingen.Client.Tests.Organisations.Support;

public class OrganisationTestData(TestFaker faker)
{
    public Guid Id { get; } = faker.Guid();
}
