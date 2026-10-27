namespace Pingen.Client.Tests.Organisations.Support;

// Interpolated into organisation documents, so no value needs JSON escaping
public class OrganisationTestData(TestFaker faker)
{
    private static readonly string[] Names = ["ACME GmbH Zürich", "Bäckerei Müller AG", "Société Générale Genève SA", "Ørsted & Søn ApS"];

    public Guid Id { get; } = faker.Guid();

    public Guid OtherId { get; } = faker.Guid();

    public string Name { get; } = faker.PickRandom(Names);
}
