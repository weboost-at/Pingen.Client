namespace Pingen.Client.Deliveries;

/// <summary>
/// Where a letter, email or ebill entered Pingen - the spec declares this set closed, unlike the delivery statuses.
/// </summary>
public static class DeliverySource
{
    /// <summary>
    /// Created in the Pingen web app.
    /// </summary>
    public const string App = "app";

    /// <summary>
    /// Created through the API.
    /// </summary>
    public const string Api = "api";

    /// <summary>
    /// Created as part of a batch.
    /// </summary>
    public const string Batch = "batch";

    /// <summary>
    /// Created by the email integration.
    /// </summary>
    public const string IntegrationEmail = "integration_email";

    /// <summary>
    /// Created by the S3 integration.
    /// </summary>
    public const string IntegrationS3 = "integration_s3";

    /// <summary>
    /// Created by the Dropbox integration.
    /// </summary>
    public const string IntegrationDropbox = "integration_dropbox";

    /// <summary>
    /// Created by the Google Drive integration.
    /// </summary>
    public const string IntegrationGoogleDrive = "integration_googledrive";

    /// <summary>
    /// Created by the OneDrive integration.
    /// </summary>
    public const string IntegrationOneDrive = "integration_onedrive";
}
