namespace Pingen.Client.Tests.Testing.Bases;

public abstract class UnitTestBase
{
    public TestDataCollection Data { get; } = new(TestFaker.ForCurrentTest());

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;
}
