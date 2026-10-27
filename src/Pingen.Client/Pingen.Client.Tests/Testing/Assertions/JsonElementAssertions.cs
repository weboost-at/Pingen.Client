using System.Text.Json;
using FluentAssertions;

namespace Pingen.Client.Tests.Testing.Assertions;

public class JsonElementAssertions(JsonElement element)
{
    public JsonElement Has(string name, string? expected)
    {
        element.GetProperty(name).GetString().Should().Be(expected);
        return element;
    }

    public JsonElement Lacks(string name)
    {
        element.TryGetProperty(name, out _).Should().BeFalse($"{name} should not be on the wire");
        return element;
    }
}

public static class JsonElementAssertionExtensions
{
    extension(JsonElement element)
    {
        public JsonElementAssertions Assert => new(element);
    }
}
