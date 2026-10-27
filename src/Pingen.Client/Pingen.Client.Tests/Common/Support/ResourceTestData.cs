namespace Pingen.Client.Tests.Common.Support;

public class ResourceTestData(TestFaker faker)
{
    public Guid Id { get; } = faker.Guid();

    public Guid ParentId { get; } = faker.Guid();
}
