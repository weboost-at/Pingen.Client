using System.Text.Json.Serialization;

namespace Pingen.Client.Batches;

/// <summary>
/// What a batch takes with it when it is deleted - the delete endpoint requires this body.
/// </summary>
public record BatchDeleteOptions
{
    /// <summary>
    /// Whether the letters the batch produced are deleted along with it.
    /// </summary>
    [JsonPropertyName("with_letters")]
    public required bool WithLetters { get; init; }

    /// <summary>
    /// Whether the deliveries the batch produced are deleted along with it.
    /// </summary>
    [JsonPropertyName("with_deliverables")]
    public bool? WithDeliverables { get; init; }
}
