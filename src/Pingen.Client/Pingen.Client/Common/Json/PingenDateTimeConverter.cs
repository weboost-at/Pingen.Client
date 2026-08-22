using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pingen.Client.Common.Json;

/// <summary>
/// Reads and writes Pingen timestamps, whose offset carries no colon (<c>2021-11-19T09:42:48+0100</c>) and so defeats
/// the built-in converter.
/// </summary>
public class PingenDateTimeConverter : JsonConverter<DateTimeOffset>
{
    /// <summary>
    /// The format Pingen timestamps are written in.
    /// </summary>
    public const string WireFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

    /// <summary>
    /// Reads a timestamp, accepting both the colon-less <c>+0100</c> Pingen writes and the ISO <c>+01:00</c> form.
    /// </summary>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateTimeOffset.Parse(reader.GetString() ?? "", CultureInfo.InvariantCulture);

    /// <summary>
    /// Writes the timestamp in the <see cref="WireFormat"/> the API accepts.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(WireFormat, CultureInfo.InvariantCulture));
}

/// <summary>
/// The nullable twin of <see cref="PingenDateTimeConverter"/>, reading the empty strings Pingen sends for timestamps
/// that have not happened yet.
/// </summary>
public class PingenNullableDateTimeConverter : JsonConverter<DateTimeOffset?>
{
    /// <summary>
    /// Reads a timestamp that may not have happened yet, mapping the empty string Pingen sends for it to <c>null</c>.
    /// </summary>
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() is { Length: > 0 } value ? DateTimeOffset.Parse(value, CultureInfo.InvariantCulture) : null;

    /// <summary>
    /// Writes the timestamp in the <see cref="PingenDateTimeConverter.WireFormat"/> the API accepts, or JSON
    /// <c>null</c> when it has no value.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is { } instant) writer.WriteStringValue(instant.ToString(PingenDateTimeConverter.WireFormat, CultureInfo.InvariantCulture));
        else writer.WriteNullValue();
    }
}
