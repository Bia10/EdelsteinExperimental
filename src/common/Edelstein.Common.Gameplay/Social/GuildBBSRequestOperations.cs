namespace Edelstein.Common.Gameplay.Social;

/// <summary>
/// Sub-operation codes written as the first byte of inbound
/// <c>CP_GuildBBS (0xB3)</c> packets sent from the client.
/// Values confirmed from <c>game_pseudocode.c:802961+</c> (V95).
/// </summary>
public enum GuildBBSRequestOperations : byte
{
    /// <summary>
    /// Write a new post or modify an existing one (combined opcode).
    /// <para>
    /// Payload: <c>bModify(1)</c> +
    /// <i>if bModify:</i> <c>nCurEntryID(4)</c> +
    /// <c>bNotice(1)</c> + <c>sTitle(str)</c> + <c>sText(str)</c> + <c>nEmoticonID(4)</c>.
    /// </para>
    /// </summary>
    Register = 0x00,

    /// <summary>
    /// Delete an existing post (author or guild master).
    /// Payload: <c>nCurEntryID(4)</c>.
    /// </summary>
    Delete = 0x01,

    /// <summary>
    /// Request the BBS post list (a page of non-notice entries).
    /// Payload: <c>nEntryListStart(4)</c> — 0-based index, always a multiple of 10.
    /// </summary>
    LoadList = 0x02,

    /// <summary>
    /// Request the full content of a single post.
    /// Payload: <c>nViewRequestEntryID(4)</c>.
    /// The client immediately follows this with a <see cref="LoadList"/> request.
    /// </summary>
    ViewEntry = 0x03,

    /// <summary>
    /// Add a comment to a post.
    /// Payload: <c>nCurEntryID(4)</c> + <c>sComment(str)</c>.
    /// </summary>
    WriteComment = 0x04,

    /// <summary>
    /// Delete a comment on a post (comment author or guild master).
    /// Payload: <c>nCurEntryID(4)</c> + <c>m_nSN(4)</c> (comment serial number).
    /// </summary>
    DeleteComment = 0x05,
}

