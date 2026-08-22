using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pingen.Client.Common.Json;

/// <summary>
/// The serializer configuration every Pingen request and response goes through.
/// </summary>
public static class PingenJson
{
    /// <summary>
    /// Options wired with the Pingen timestamp and enum conventions, omitting null members when writing.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Relaxed escaping keeps timestamps and umlauts on the wire as the API documents them - a JSON body is never an HTML context.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new PingenDateTimeConverter(),
            new PingenNullableDateTimeConverter(),
            new PingenEnumConverter(),
        },
    };
}
