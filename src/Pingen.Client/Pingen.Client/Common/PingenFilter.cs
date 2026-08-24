using System.Globalization;
using System.Text.Json;
using Pingen.Client.Common.Json;

namespace Pingen.Client.Common;

/// <summary>
/// A filter expression for the list endpoints, rendered into the JSON the <c>filter</c> query parameter carries - the
/// attribute names live on the resource's field class such as <see cref="Deliveries.Letters.LetterField"/>.
/// </summary>
public record PingenFilter
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly string _json;

    private PingenFilter(string json) => _json = json;

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> equals <paramref name="value"/>.
    /// </summary>
    public static PingenFilter Where(string attribute, string value) => Compare(attribute, "", value);

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> equals the given date.
    /// </summary>
    public static PingenFilter Where(string attribute, DateOnly value) => Compare(attribute, "", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> equals the given timestamp.
    /// </summary>
    public static PingenFilter Where(string attribute, DateTimeOffset value) => Compare(attribute, "", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> differs from <paramref name="value"/>.
    /// </summary>
    public static PingenFilter Not(string attribute, string value) => Compare(attribute, "!", value);

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> contains <paramref name="value"/>.
    /// </summary>
    public static PingenFilter Contains(string attribute, string value) => Compare(attribute, "~", value);

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is greater than <paramref name="value"/>.
    /// </summary>
    public static PingenFilter GreaterThan(string attribute, string value) => Compare(attribute, ">", value);

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is later than the given date.
    /// </summary>
    public static PingenFilter GreaterThan(string attribute, DateOnly value) => Compare(attribute, ">", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is later than the given timestamp.
    /// </summary>
    public static PingenFilter GreaterThan(string attribute, DateTimeOffset value) => Compare(attribute, ">", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is greater than or equal to <paramref name="value"/>.
    /// </summary>
    public static PingenFilter GreaterOrEqual(string attribute, string value) => Compare(attribute, ">=", value);

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is on or after the given date.
    /// </summary>
    public static PingenFilter GreaterOrEqual(string attribute, DateOnly value) => Compare(attribute, ">=", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is at or after the given timestamp.
    /// </summary>
    public static PingenFilter GreaterOrEqual(string attribute, DateTimeOffset value) => Compare(attribute, ">=", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is less than <paramref name="value"/>.
    /// </summary>
    public static PingenFilter LessThan(string attribute, string value) => Compare(attribute, "<", value);

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is earlier than the given date.
    /// </summary>
    public static PingenFilter LessThan(string attribute, DateOnly value) => Compare(attribute, "<", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is earlier than the given timestamp.
    /// </summary>
    public static PingenFilter LessThan(string attribute, DateTimeOffset value) => Compare(attribute, "<", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is less than or equal to <paramref name="value"/>.
    /// </summary>
    public static PingenFilter LessOrEqual(string attribute, string value) => Compare(attribute, "<=", value);

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is on or before the given date.
    /// </summary>
    public static PingenFilter LessOrEqual(string attribute, DateOnly value) => Compare(attribute, "<=", Format(value));

    /// <summary>
    /// Matches resources whose <paramref name="attribute"/> is at or before the given timestamp.
    /// </summary>
    public static PingenFilter LessOrEqual(string attribute, DateTimeOffset value) => Compare(attribute, "<=", Format(value));

    /// <summary>
    /// Matches resources satisfying every one of <paramref name="filters"/>.
    /// </summary>
    public static PingenFilter And(params PingenFilter[] filters) => Combine("and", filters);

    /// <summary>
    /// Matches resources satisfying any one of <paramref name="filters"/>.
    /// </summary>
    public static PingenFilter Or(params PingenFilter[] filters) => Combine("or", filters);

    /// <summary>
    /// Wraps filter JSON written by hand, emitted verbatim.
    /// </summary>
    public static PingenFilter Raw(string json) => new(json);

    /// <summary>
    /// Renders the filter as the JSON string the <c>filter</c> query parameter carries.
    /// </summary>
    public string ToJson() => _json;

    private static PingenFilter Compare(string attribute, string @operator, string value) =>
        new($"{{{JsonSerializer.Serialize(attribute, PingenJson.Options)}:{JsonSerializer.Serialize(@operator + value, PingenJson.Options)}}}");

    private static PingenFilter Combine(string combinator, PingenFilter[] filters) =>
        new($"{{\"{combinator}\":[{string.Join(',', filters.Select(filter => filter._json))}]}}");

    private static string Format(DateOnly value) => value.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static string Format(DateTimeOffset value) => value.ToString(PingenDateTimeConverter.WireFormat, CultureInfo.InvariantCulture);
}
