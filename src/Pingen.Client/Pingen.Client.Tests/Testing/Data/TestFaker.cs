namespace Pingen.Client.Tests.Testing.Data;

// Bogus is not among the dependencies the library may take, so the catalog draws from a seeded Random instead
public class TestFaker(int seed)
{
    private const uint FnvOffsetBasis = 2166136261;

    private const uint FnvPrime = 16777619;

    private const string AlphaNumerics = "abcdefghijklmnopqrstuvwxyz0123456789";

    private readonly Random _random = new(seed);

    // Seeded from the running test's identity, so a test draws the same values on every run
    public static TestFaker ForCurrentTest() => new(Seed(TestContext.Current.Test?.UniqueID ?? ""));

    public Guid Guid()
    {
        Span<byte> bytes = stackalloc byte[16];
        _random.NextBytes(bytes);

        return new(bytes);
    }

    public int Int(int min, int max) => _random.Next(min, max + 1);

    public T PickRandom<T>(params ReadOnlySpan<T> items) => items[_random.Next(items.Length)];

    public string AlphaNumeric(int length) => new(_random.GetItems(AlphaNumerics.AsSpan(), length));

    public byte[] Bytes(int length)
    {
        var bytes = new byte[length];
        _random.NextBytes(bytes);

        return bytes;
    }

    // string.GetHashCode differs between processes, FNV-1a gives a test the same seed on every run and machine
    private static int Seed(string identity)
    {
        var hash = FnvOffsetBasis;
        foreach (var character in identity) hash = (hash ^ character) * FnvPrime;

        return unchecked((int)hash);
    }
}
