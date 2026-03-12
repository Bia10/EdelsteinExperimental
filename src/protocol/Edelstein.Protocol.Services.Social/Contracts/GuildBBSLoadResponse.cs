namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>
/// Returns the BBS list result for a guild.
/// <para>
/// The notice (<see cref="Notice"/>) is a single optional pinned post transmitted
/// via the <c>has_notice</c> prefix byte in <c>LP_GuildBBS</c> case 6. It is
/// always sent separately from the page entries and the client stores it in a
/// dedicated <c>m_Notice</c> slot.
/// </para>
/// <para>
/// <see cref="TotalCount"/> is the total number of <b>non-notice</b> posts
/// (<c>nEntryListTotalCount</c>) used by the client to compute the page selector range.
/// </para>
/// </summary>
public record GuildBBSLoadResponse(
    GuildResult Result = GuildResult.Success,
    IGuildBBSPost? Notice = null,
    IReadOnlyList<IGuildBBSPost>? Posts = null,
    int TotalCount = 0
);
