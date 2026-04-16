using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Services.Social;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Packets;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

/// <summary>
/// Handles inbound <c>CP_GuildBBS (0xB3)</c> packets from the client.
/// Dispatches each sub-opcode to the guild service and sends back the
/// appropriate <c>LP_GuildBBS (0x3B)</c> response directly to the requesting user.
///
/// Sub-opcodes (confirmed from <c>game_pseudocode.c:802961+</c> V95):
/// <list type="bullet">
///   <item><term>0x00 (Register)</term><description>
///     Write new post or modify existing (bModify flag).
///     Sends sub-op 7 (ViewEntryResult).
///   </description></item>
///   <item><term>0x01 (Delete)</term><description>
///     Delete a post. Sends sub-op 6 (LoadListResult, page 0).
///   </description></item>
///   <item><term>0x02 (LoadList)</term><description>
///     Request a page of the post list. Sends sub-op 6 (LoadListResult).
///   </description></item>
///   <item><term>0x03 (ViewEntry)</term><description>
///     View a single post. Sends sub-op 7 (ViewEntryResult) or 8 (EntryNotFound).
///   </description></item>
///   <item><term>0x04 (WriteComment)</term><description>
///     Add a comment. Sends sub-op 7 (ViewEntryResult).
///   </description></item>
///   <item><term>0x05 (DeleteComment)</term><description>
///     Remove a comment. Sends sub-op 7 (ViewEntryResult).
///   </description></item>
/// </list>
/// </summary>
public class GuildBBSHandler : AbstractFieldHandler
{
    private readonly ILogger _logger;

    public GuildBBSHandler(ILogger<GuildBBSHandler> logger) => _logger = logger;

    public override short Operation => (short)PacketRecvOperations.GuildBBS;

    protected override async Task Handle(IFieldUser user, IPacketReader reader)
    {
        if (user.StageUser.Guild == null)
            return;

        var guild = user.StageUser.Guild;
        var guildService = user.StageUser.Context.Services.Guild;
        var type = (GuildBBSRequestOperations)reader.ReadByte();

        switch (type)
        {
            // ── 0x00: Register (write new post or modify existing) ─────────────
            case GuildBBSRequestOperations.Register:
            {
                var bModify = reader.ReadByte() != 0;
                var articleID = bModify ? reader.ReadInt() : (int?)null;
                var isNotice = reader.ReadByte() != 0;
                var title = reader.ReadString();
                var content = reader.ReadString();
                var emoticonID = reader.ReadInt();

                if (bModify && articleID.HasValue)
                {
                    var response = await guildService.BBSEdit(
                        new GuildBBSEditRequest(
                            guild.ID,
                            articleID.Value,
                            user.Character.ID,
                            isNotice,
                            title,
                            content,
                            emoticonID
                        )
                    );

                    if (response.Result == GuildResult.FailedPostNotFound)
                    {
                        await SendEntryNotFound(user);
                        return;
                    }
                    if (response.Result != GuildResult.Success || response.Post == null)
                        return;
                    await SendViewEntryResult(user, response.Post, response.Comments!);
                }
                else
                {
                    var response = await guildService.BBSWrite(
                        new GuildBBSWriteRequest(
                            guild.ID,
                            user.Character.ID,
                            user.Character.Name,
                            isNotice,
                            title,
                            content,
                            emoticonID
                        )
                    );

                    if (response.Result != GuildResult.Success || response.Post == null)
                        return;
                    await SendViewEntryResult(user, response.Post, response.Comments!);
                }
                break;
            }

            // ── 0x01: Delete ─────────────────────────────────────────────────
            case GuildBBSRequestOperations.Delete:
            {
                var articleID = reader.ReadInt();
                var deleteResponse = await guildService.BBSDelete(
                    new GuildBBSDeleteRequest(guild.ID, articleID, user.Character.ID)
                );

                if (deleteResponse.Result != GuildResult.Success)
                    return;

                var loadResponse = await guildService.BBSLoad(new GuildBBSLoadRequest(guild.ID));
                if (loadResponse.Result == GuildResult.Success)
                    await SendLoadListResult(
                        user,
                        loadResponse.Notice,
                        loadResponse.Posts!,
                        loadResponse.TotalCount
                    );
                break;
            }

            // ── 0x02: LoadList ───────────────────────────────────────────────
            case GuildBBSRequestOperations.LoadList:
            {
                var entryListStart = reader.ReadInt();
                var response = await guildService.BBSLoad(
                    new GuildBBSLoadRequest(guild.ID, entryListStart)
                );
                if (response.Result != GuildResult.Success)
                    return;
                await SendLoadListResult(
                    user,
                    response.Notice,
                    response.Posts!,
                    response.TotalCount
                );
                break;
            }

            // ── 0x03: ViewEntry ──────────────────────────────────────────────
            case GuildBBSRequestOperations.ViewEntry:
            {
                var articleID = reader.ReadInt();
                var response = await guildService.BBSView(
                    new GuildBBSViewRequest(guild.ID, articleID)
                );

                if (response.Result == GuildResult.FailedPostNotFound)
                {
                    await SendEntryNotFound(user);
                    return;
                }
                if (response.Result != GuildResult.Success || response.Post == null)
                    return;
                await SendViewEntryResult(user, response.Post, response.Comments!);
                break;
            }

            // ── 0x04: WriteComment ───────────────────────────────────────────
            case GuildBBSRequestOperations.WriteComment:
            {
                var articleID = reader.ReadInt();
                var content = reader.ReadString();

                var response = await guildService.BBSWriteComment(
                    new GuildBBSWriteCommentRequest(
                        guild.ID,
                        articleID,
                        user.Character.ID,
                        user.Character.Name,
                        content
                    )
                );

                if (response.Result == GuildResult.FailedPostNotFound)
                {
                    await SendEntryNotFound(user);
                    return;
                }
                if (response.Result != GuildResult.Success || response.Post == null)
                    return;
                await SendViewEntryResult(user, response.Post, response.Comments!);
                break;
            }

            // ── 0x05: DeleteComment ──────────────────────────────────────────
            case GuildBBSRequestOperations.DeleteComment:
            {
                var articleID = reader.ReadInt();
                var commentSN = reader.ReadInt(); // m_nSN (comment serial number / ID)

                var response = await guildService.BBSDeleteComment(
                    new GuildBBSDeleteCommentRequest(
                        guild.ID,
                        articleID,
                        commentSN,
                        user.Character.ID
                    )
                );

                if (response.Result == GuildResult.FailedPostNotFound)
                {
                    await SendEntryNotFound(user);
                    return;
                }
                if (response.Result != GuildResult.Success || response.Post == null)
                    return;
                await SendViewEntryResult(user, response.Post, response.Comments!);
                break;
            }

            default:
                _logger.LogDebug(
                    "Unhandled CP_GuildBBS sub-opcode 0x{Type:X2} from character {Name}",
                    (byte)type,
                    user.Character.Name
                );
                break;
        }
    }

    // ── LP_GuildBBS sub-op 6: LoadListResult ──────────────────────────────────
    //
    // Wire layout (game_pseudocode.c:803710):
    //   has_notice(1)
    //   [if has_notice=1]:
    //     nEntryID(4) | nCharacterID(4) | sTitle(str) | ftDate(8) | nEmoticon(4) | nComments(4)
    //   nEntryListTotalCount(4)  — total non-notice posts (for page selector)
    //   nPageEntryCount(4)       — entries in this page (≤10)
    //   [per entry — same layout as notice]:
    //     nEntryID(4) | nCharacterID(4) | sTitle(str) | ftDate(8) | nEmoticon(4) | nComments(4)
    //
    // ⚠ nCharacterID is decoded BEFORE sTitle; nEmoticon is decoded BEFORE nComments.

    private static Task SendLoadListResult(
        IFieldUser user,
        IGuildBBSPost? notice,
        IReadOnlyList<IGuildBBSPost> posts,
        int totalCount
    )
    {
        using var packet = new PacketWriter(PacketSendOperations.GuildBBS);
        packet.WriteByte((byte)GuildBBSOperations.LoadListResult);

        if (notice != null)
        {
            packet.WriteByte(1);
            WriteEntryListEntry(packet, notice);
        }
        else
        {
            packet.WriteByte(0);
        }

        packet.WriteInt(totalCount); // nEntryListTotalCount
        packet.WriteInt(posts.Count); // nPageEntryCount
        foreach (var post in posts)
            WriteEntryListEntry(packet, post);

        return user.Dispatch(packet.Build());
    }

    // ── LP_GuildBBS sub-op 7: ViewEntryResult ──────────────────────────────────
    //
    // Wire layout (game_pseudocode.c:805097):
    //   nCurEntryID(4) | nCurCharacterID(4) | ftCurDate(8) | sCurTitle(str) | sCurText(str)
    //   | nEmoticon(4) | nCommentCount(4)
    //   [per comment]:
    //     m_nSN(4) | m_nCharacterID(4) | m_ftDate(8) | m_sComment(str)
    //
    // ⚠ ftCurDate decoded BEFORE title/text; m_ftDate decoded BEFORE m_sComment.

    private static Task SendViewEntryResult(
        IFieldUser user,
        IGuildBBSPost post,
        IReadOnlyList<IGuildBBSComment> comments
    )
    {
        using var packet = new PacketWriter(PacketSendOperations.GuildBBS);
        packet.WriteByte((byte)GuildBBSOperations.ViewEntryResult);
        packet.WriteInt(post.ID);
        packet.WriteInt(post.AuthorID);
        packet.WriteDateTime(post.CreatedAt); // ftCurDate — BEFORE strings
        packet.WriteString(post.Title);
        packet.WriteString(post.Content);
        packet.WriteInt(post.EmoticonID);
        packet.WriteInt(comments.Count);
        foreach (var comment in comments)
        {
            packet.WriteInt(comment.ID); // m_nSN
            packet.WriteInt(comment.AuthorID);
            packet.WriteDateTime(comment.CreatedAt); // m_ftDate — BEFORE text
            packet.WriteString(comment.Content);
        }
        return user.Dispatch(packet.Build());
    }

    // ── LP_GuildBBS sub-op 8: EntryNotFound ────────────────────────────────────
    // No payload. CUIGuildBBS::OnEntryNotFound(void) shows StringPool 0xEC7.

    private static Task SendEntryNotFound(IFieldUser user)
    {
        using var packet = new PacketWriter(PacketSendOperations.GuildBBS);
        packet.WriteByte((byte)GuildBBSOperations.EntryNotFound);
        return user.Dispatch(packet.Build());
    }

    // ENTRYLIST entry helper — used for both notice and page entries.
    // Layout: nEntryID(4) | nCharacterID(4) | sTitle(str) | ftDate(8) | nEmoticon(4) | nComments(4)
    private static void WriteEntryListEntry(IPacketWriter packet, IGuildBBSPost post)
    {
        packet.WriteInt(post.ID);
        packet.WriteInt(post.AuthorID); // nCharacterID — before sTitle
        packet.WriteString(post.Title);
        packet.WriteDateTime(post.CreatedAt); // ftDate (_FILETIME)
        packet.WriteInt(post.EmoticonID); // nEmoticon — before nComments
        packet.WriteInt(post.CommentCount);
    }
}
