namespace Pingen.Client.Tests.Testing.Data;

// Default converts to T for builders, but Should().Be takes object - an assertion names the variant
public class TestValue<T>(T @default, T updated)
{
    public T Default { get; } = @default;

    public T Updated { get; } = updated;

    public static implicit operator T(TestValue<T> value) => value.Default;
}
