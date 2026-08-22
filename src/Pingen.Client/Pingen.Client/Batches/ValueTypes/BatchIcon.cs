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
    [JsonStringEnumMemberName("campaign")]
    Campaign,

    /// <summary>
    /// A megaphone.
    /// </summary>
    [JsonStringEnumMemberName("megaphone")]
    Megaphone,

    /// <summary>
    /// A waving hand.
    /// </summary>
    [JsonStringEnumMemberName("wave-hand")]
    WaveHand,

    /// <summary>
    /// A flash.
    /// </summary>
    [JsonStringEnumMemberName("flash")]
    Flash,

    /// <summary>
    /// A rocket.
    /// </summary>
    [JsonStringEnumMemberName("rocket")]
    Rocket,

    /// <summary>
    /// A bell.
    /// </summary>
    [JsonStringEnumMemberName("bell")]
    Bell,

    /// <summary>
    /// A percent tag.
    /// </summary>
    [JsonStringEnumMemberName("percent-tag")]
    PercentTag,

    /// <summary>
    /// A percent badge.
    /// </summary>
    [JsonStringEnumMemberName("percent-badge")]
    PercentBadge,

    /// <summary>
    /// A present.
    /// </summary>
    [JsonStringEnumMemberName("present")]
    Present,

    /// <summary>
    /// A receipt.
    /// </summary>
    [JsonStringEnumMemberName("receipt")]
    Receipt,

    /// <summary>
    /// A document.
    /// </summary>
    [JsonStringEnumMemberName("document")]
    Document,

    /// <summary>
    /// An information sign.
    /// </summary>
    [JsonStringEnumMemberName("information")]
    Information,

    /// <summary>
    /// A calendar.
    /// </summary>
    [JsonStringEnumMemberName("calendar")]
    Calendar,

    /// <summary>
    /// A newspaper.
    /// </summary>
    [JsonStringEnumMemberName("newspaper")]
    Newspaper,

    /// <summary>
    /// A crown.
    /// </summary>
    [JsonStringEnumMemberName("crown")]
    Crown,

    /// <summary>
    /// A virus.
    /// </summary>
    [JsonStringEnumMemberName("virus")]
    Virus,
}
