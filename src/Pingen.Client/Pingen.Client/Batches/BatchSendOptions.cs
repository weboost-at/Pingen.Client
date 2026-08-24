using System.Text.Json.Serialization;
using Pingen.Client.Deliveries.ValueTypes;

namespace Pingen.Client.Batches;

/// <summary>
/// How a batch is dispatched when it is sent - the API takes a different resource type per channel, so the channel is
/// picked by factory instead of by property.
/// </summary>
public record BatchSendOptions
{
    private BatchSendOptions(string type, BatchDeliveryProduct product)
    {
        Type = type;
        Product = product;
    }

    /// <summary>
    /// The JSON:API type the send request is written as - <c>batches_channel_post_send</c>,
    /// <c>batches_channel_email_send</c> or <c>batches_channel_ebill_send</c>.
    /// </summary>
    [JsonIgnore]
    public string Type { get; }

    // Named after the concept rather than the wire field - a member called DeliveryProduct would shadow the enum of that name in this type.
    /// <summary>
    /// The product the deliveries of the batch are dispatched with.
    /// </summary>
    [JsonPropertyName("delivery_product")]
    public BatchDeliveryProduct Product { get; }

    /// <summary>
    /// Which sides of the paper are printed, sent on post batches only.
    /// </summary>
    [JsonPropertyName("print_mode")]
    public PrintMode? PrintMode { get; private init; }

    /// <summary>
    /// Which colors are printed, sent on post batches only.
    /// </summary>
    [JsonPropertyName("print_spectrum")]
    public PrintSpectrum? PrintSpectrum { get; private init; }

    /// <summary>
    /// Dispatches the batch as physical mail.
    /// </summary>
    public static BatchSendOptions Post(DeliveryProduct deliveryProduct, PrintMode printMode, PrintSpectrum printSpectrum) =>
        new(
            type: "batches_channel_post_send",
            product: ToBatchProduct(deliveryProduct)
        )
        {
            PrintMode = printMode,
            PrintSpectrum = printSpectrum,
        };

    /// <summary>
    /// Dispatches the batch as email, the only product of that channel.
    /// </summary>
    public static BatchSendOptions Email() =>
        new(
            type: "batches_channel_email_send",
            product: BatchDeliveryProduct.ElectronicEmail
        );

    /// <summary>
    /// Dispatches the batch as ebills, the only product of that channel.
    /// </summary>
    public static BatchSendOptions Ebill() =>
        new(
            type: "batches_channel_ebill_send",
            product: BatchDeliveryProduct.ElectronicEbill
        );

    private static BatchDeliveryProduct ToBatchProduct(DeliveryProduct product) => product switch
    {
        DeliveryProduct.Fast => BatchDeliveryProduct.Fast,
        DeliveryProduct.Cheap => BatchDeliveryProduct.Cheap,
        DeliveryProduct.Bulk => BatchDeliveryProduct.Bulk,
        DeliveryProduct.Premium => BatchDeliveryProduct.Premium,
        DeliveryProduct.Registered => BatchDeliveryProduct.Registered,
        _ => throw new ArgumentOutOfRangeException(nameof(product), product, "The post channel of a batch knows no such delivery product."),
    };
}

/// <summary>
/// The product a batch is dispatched with, which adds the electronic products the postal <see cref="DeliveryProduct"/>
/// does not carry.
/// </summary>
public enum BatchDeliveryProduct
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

    /// <summary>
    /// Email, the product of the email channel.
    /// </summary>
    [JsonStringEnumMemberName(DeliveryProductValue.ElectronicEmail)]
    ElectronicEmail,

    /// <summary>
    /// Ebill, the product of the ebill channel.
    /// </summary>
    [JsonStringEnumMemberName(DeliveryProductValue.ElectronicEbill)]
    ElectronicEbill,
}
