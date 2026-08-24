using System.Text.Json.Serialization;

namespace Pingen.Client.Deliveries.ValueTypes;

/// <summary>
/// The postal product a delivery is dispatched with.
/// </summary>
public enum DeliveryProduct
{
    /// <summary>
    /// Priority mail.
    /// </summary>
    [JsonStringEnumMemberName(DeliveryProductValue.Fast)]
    Fast,

    /// <summary>
    /// Economy mail.
    /// </summary>
    [JsonStringEnumMemberName(DeliveryProductValue.Cheap)]
    Cheap,

    /// <summary>
    /// Bulk mail.
    /// </summary>
    [JsonStringEnumMemberName(DeliveryProductValue.Bulk)]
    Bulk,

    /// <summary>
    /// Premium mail.
    /// </summary>
    [JsonStringEnumMemberName(DeliveryProductValue.Premium)]
    Premium,

    /// <summary>
    /// Registered mail.
    /// </summary>
    [JsonStringEnumMemberName(DeliveryProductValue.Registered)]
    Registered,
}

/// <summary>
/// The wire values a delivery product serializes to, carrying the two electronic products a batch channel assigns on
/// top of the five <see cref="DeliveryProduct"/> knows.
/// </summary>
public static class DeliveryProductValue
{
    /// <summary>
    /// Priority mail.
    /// </summary>
    public const string Fast = "fast";

    /// <summary>
    /// Economy mail.
    /// </summary>
    public const string Cheap = "cheap";

    /// <summary>
    /// Bulk mail.
    /// </summary>
    public const string Bulk = "bulk";

    /// <summary>
    /// Premium mail.
    /// </summary>
    public const string Premium = "premium";

    /// <summary>
    /// Registered mail.
    /// </summary>
    public const string Registered = "registered";

    /// <summary>
    /// Email, assigned to the deliveries of an email batch.
    /// </summary>
    public const string ElectronicEmail = "electronic_email";

    /// <summary>
    /// Ebill, assigned to the deliveries of an ebill batch.
    /// </summary>
    public const string ElectronicEbill = "electronic_ebill";
}
