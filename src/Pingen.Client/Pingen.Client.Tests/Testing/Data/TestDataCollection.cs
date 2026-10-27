using Pingen.Client.Tests.Common.Support;
using Pingen.Client.Tests.Deliveries.Ebills.Support;
using Pingen.Client.Tests.Deliveries.Emails.Support;
using Pingen.Client.Tests.Deliveries.Letters.Support;
using Pingen.Client.Tests.Files.Support;
using Pingen.Client.Tests.Organisations.Support;
using Pingen.Client.Tests.Users.Support;

namespace Pingen.Client.Tests.Testing.Data;

// One get-only property per slice, each `new(faker)`: a `=>` property would draw a new data set on every read
public class TestDataCollection(TestFaker faker)
{
    public EbillTestData Ebill { get; } = new(faker);

    public EmailTestData Email { get; } = new(faker);

    public ErrorTestData Error { get; } = new(faker);

    public FileTestData File { get; } = new(faker);

    public LetterTestData Letter { get; } = new(faker);

    public OrganisationTestData Organisation { get; } = new(faker);

    public RequestTestData Request { get; } = new(faker);

    public UserTestData User { get; } = new(faker);
}
