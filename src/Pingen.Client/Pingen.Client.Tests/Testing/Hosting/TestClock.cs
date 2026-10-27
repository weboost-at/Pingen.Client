namespace Pingen.Client.Tests.Testing.Hosting;

// Stands still until a test moves it, so a token's lifetime is exact to the tick
public class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; private set; } = new(2026, 1, 15, 8, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}
