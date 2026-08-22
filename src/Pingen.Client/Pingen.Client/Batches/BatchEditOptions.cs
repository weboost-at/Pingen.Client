using System.Text.Json.Serialization;
using Pingen.Client.Batches.ValueTypes;

namespace Pingen.Client.Batches;

/// <summary>
/// What may be changed on a batch that has not been sent yet.
/// </summary>
public record BatchEditOptions
{
    /// <summary>
    /// The new name of the batch - between 5 and 100 characters, left unchanged when null.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The new icon of the batch, left unchanged when null.
    /// </summary>
    [JsonPropertyName("icon")]
    public BatchIcon? Icon { get; init; }
}
