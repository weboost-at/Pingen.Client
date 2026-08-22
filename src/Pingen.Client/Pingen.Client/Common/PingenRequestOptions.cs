namespace Pingen.Client.Common;

/// <summary>
/// Options applied to a single mutating request.
/// </summary>
public record PingenRequestOptions
{
    /// <summary>
    /// Key that makes Pingen replay the original response instead of repeating the operation - 1 to 64 characters, kept
    /// for 24 hours, a UUIDv4 is the suggested shape.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}
