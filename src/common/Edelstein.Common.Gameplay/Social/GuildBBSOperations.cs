namespace Edelstein.Common.Gameplay.Social;

/// <summary>
/// Sub-operation codes written as the first byte of outbound
/// <c>LP_GuildBBS (0x3B)</c> packets sent from server to client.
/// Values taken verbatim from the V95 client dispatch (game_pseudocode.c:806409).
/// </summary>
public enum GuildBBSOperations : byte
{
    /// <summary>
    /// Delivers a paged list of BBS posts to the client (<c>CUIGuildBBS::OnLoadListResult</c>).
    /// <para>Wire layout (<c>game_pseudocode.c:803710</c>):</para>
    /// <code>
    /// has_notice(1)
    /// [if has_notice=1]: nEntryID(4) | nCharacterID(4) | sTitle(str) | ftDate(8) | nEmoticon(4) | nComments(4)
    /// nEntryListTotalCount(4)
    /// nPageEntryCount(4)
    /// [per entry]: nEntryID(4) | nCharacterID(4) | sTitle(str) | ftDate(8) | nEmoticon(4) | nComments(4)
    /// </code>
    /// ⚠ <c>nCharacterID</c> is decoded BEFORE <c>sTitle</c>; <c>nEmoticon</c> BEFORE <c>nComments</c>.
    /// </summary>
    LoadListResult = 0x06,

    /// <summary>
    /// Delivers the full content of a single post with its comments (<c>CUIGuildBBS::OnViewEntryResult</c>).
    /// <para>Wire layout (<c>game_pseudocode.c:805097</c>):</para>
    /// <code>
    /// nCurEntryID(4) | nCurCharacterID(4) | ftCurDate(8) | sCurTitle(str) | sCurText(str)
    /// | nEmoticon(4) | nCommentCount(4)
    /// [per comment]: m_nSN(4) | m_nCharacterID(4) | m_ftDate(8) | m_sComment(str)
    /// </code>
    /// ⚠ <c>ftCurDate</c> is decoded BEFORE title/text strings; <c>m_ftDate</c> BEFORE <c>m_sComment</c>.
    /// </summary>
    ViewEntryResult = 0x07,

    /// <summary>
    /// Notifies the client that the requested article ID does not exist (<c>CUIGuildBBS::OnEntryNotFound(void)</c>).
    /// Shows StringPool <c>0xEC7</c> and clears <c>m_CurEntry</c>. No additional payload.
    /// </summary>
    EntryNotFound = 0x08,
}
