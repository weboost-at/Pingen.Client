using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pingen.Client.Common.Json;

/// <summary>
/// Reads a flag the spec types as a boolean but documents with a numeric example, so that <c>true</c> and <c>1</c> both
/// land.
/// </summary>
public class PingenBooleanConverter : JsonConverter<bool>
{
    /// <summary>
    /// Reads the flag, treating any non-zero number as <c>true</c>.
    /// </summary>
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    {
        JsonTokenType.Number => reader.GetInt32() is not 0,
        _ => reader.GetBoolean(),
    };

    /// <summary>
    /// Writes the flag as a JSON boolean, the shape the API accepts.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
}
