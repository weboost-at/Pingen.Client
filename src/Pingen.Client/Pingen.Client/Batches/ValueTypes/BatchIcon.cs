using System.Text.Json.Serialization;

namespace Pingen.Client.Batches.ValueTypes;

/// <summary>
/// The icon a batch is shown with - the wire names are kebab-case, not snake_case.
/// </summary>
public enum BatchIcon
{
    /// <summary>
    /// A campaign.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Campaign)]
    Campaign,

    /// <summary>
    /// A megaphone.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Megaphone)]
    Megaphone,

    /// <summary>
    /// A waving hand.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.WaveHand)]
    WaveHand,

    /// <summary>
    /// A flash.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Flash)]
    Flash,

    /// <summary>
    /// A rocket.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Rocket)]
    Rocket,

    /// <summary>
    /// A bell.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Bell)]
    Bell,

    /// <summary>
    /// A percent tag.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.PercentTag)]
    PercentTag,

    /// <summary>
    /// A percent badge.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.PercentBadge)]
    PercentBadge,

    /// <summary>
    /// A present.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Present)]
    Present,

    /// <summary>
    /// A receipt.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Receipt)]
    Receipt,

    /// <summary>
    /// A document.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Document)]
    Document,

    /// <summary>
    /// An information sign.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Information)]
    Information,

    /// <summary>
    /// A calendar.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Calendar)]
    Calendar,

    /// <summary>
    /// A newspaper.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Newspaper)]
    Newspaper,

    /// <summary>
    /// A crown.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Crown)]
    Crown,

    /// <summary>
    /// A virus.
    /// </summary>
    [JsonStringEnumMemberName(BatchIconValue.Virus)]
    Virus,
}

/// <summary>
/// The wire values <see cref="BatchIcon"/> serializes to, for comparing the strings responses carry back.
/// </summary>
public static class BatchIconValue
{
    /// <summary>
    /// A campaign.
    /// </summary>
    public const string Campaign = "campaign";

    /// <summary>
    /// A megaphone.
    /// </summary>
    public const string Megaphone = "megaphone";

    /// <summary>
    /// A waving hand.
    /// </summary>
    public const string WaveHand = "wave-hand";

    /// <summary>
    /// A flash.
    /// </summary>
    public const string Flash = "flash";

    /// <summary>
    /// A rocket.
    /// </summary>
    public const string Rocket = "rocket";

    /// <summary>
    /// A bell.
    /// </summary>
    public const string Bell = "bell";

    /// <summary>
    /// A percent tag.
    /// </summary>
    public const string PercentTag = "percent-tag";

    /// <summary>
    /// A percent badge.
    /// </summary>
    public const string PercentBadge = "percent-badge";

    /// <summary>
    /// A present.
    /// </summary>
    public const string Present = "present";

    /// <summary>
    /// A receipt.
    /// </summary>
    public const string Receipt = "receipt";

    /// <summary>
    /// A document.
    /// </summary>
    public const string Document = "document";

    /// <summary>
    /// An information sign.
    /// </summary>
    public const string Information = "information";

    /// <summary>
    /// A calendar.
    /// </summary>
    public const string Calendar = "calendar";

    /// <summary>
    /// A newspaper.
    /// </summary>
    public const string Newspaper = "newspaper";

    /// <summary>
    /// A crown.
    /// </summary>
    public const string Crown = "crown";

    /// <summary>
    /// A virus.
    /// </summary>
    public const string Virus = "virus";
}
