namespace Pingen.Client.Batches;

/// <summary>
/// Where a batch entered Pingen - a batch carries only these two of the wider set a delivery can carry.
/// </summary>
public static class BatchSource
{
    /// <summary>
    /// Created in the Pingen web app.
    /// </summary>
    public const string App = "app";

    /// <summary>
    /// Created through the API.
    /// </summary>
    public const string Api = "api";
}
