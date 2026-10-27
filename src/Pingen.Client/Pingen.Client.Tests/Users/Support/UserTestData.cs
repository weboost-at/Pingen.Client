namespace Pingen.Client.Tests.Users.Support;

// Interpolated into user documents, so no value needs JSON escaping
public class UserTestData(TestFaker faker)
{
    private static readonly string[] Emails = ["jürgen@example.com", "zoë.müller@example.ch", "chloé.dubois@exemple.fr", "ömer@örnek.com.tr"];

    private static readonly string[] FirstNames = ["Jürgen", "Zoë", "Chloé", "Ömer"];

    private static readonly string[] LastNames = ["Snow", "Müller-Lüdenscheidt", "Dubois", "Çelik"];

    public Guid Id { get; } = faker.Guid();

    public Guid AssociationId { get; } = faker.Guid();

    public Guid OtherAssociationId { get; } = faker.Guid();

    public string Email { get; } = faker.PickRandom(Emails);

    public string FirstName { get; } = faker.PickRandom(FirstNames);

    public string LastName { get; } = faker.PickRandom(LastNames);
}
