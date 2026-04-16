using Edelstein.Protocol.Services.Social.Contracts;

namespace Edelstein.Protocol.Services.Social;

/// <summary>
/// Service interface for all guild lifecycle and management operations.
/// Implementations persist data via EF Core and propagate real-time changes
/// to all connected game-server instances through a message bus.
/// </summary>
public interface IGuildService
{
    /// <summary>
    /// Loads the full guild membership snapshot for the given character.
    /// Returns <c>null</c> when the character does not belong to any guild.
    /// </summary>
    Task<GuildLoadResponse> Load(GuildLoadRequest request);

    /// <summary>
    /// Checks whether the proposed guild name is available and valid.
    /// Used in the creation flow before committing.
    /// </summary>
    Task<GuildNameCheckResponse> CheckName(GuildNameCheckRequest request);

    Task<GuildResponse> Create(GuildCreateRequest request);
    Task<GuildResponse> Disband(GuildDisbandRequest request);
    Task<GuildResponse> Invite(GuildInviteRequest request);
    Task<GuildResponse> InviteAccept(GuildInviteAcceptRequest request);
    Task<GuildResponse> InviteReject(GuildInviteRejectRequest request);
    Task<GuildResponse> Leave(GuildLeaveRequest request);
    Task<GuildResponse> Kick(GuildKickRequest request);
    Task<GuildResponse> SetNotice(GuildSetNoticeRequest request);
    Task<GuildResponse> SetGradeNames(GuildSetGradeNamesRequest request);
    Task<GuildResponse> SetMemberGrade(GuildSetMemberGradeRequest request);
    Task<GuildResponse> SetMark(GuildSetMarkRequest request);
    Task<GuildResponse> IncMaxMemberNum(GuildIncMaxMemberRequest request);
    Task<GuildResponse> UpdateLevelOrJob(GuildUpdateLevelOrJobRequest request);

    /// <summary>
    /// Updates a member's online channel; use −2 to mark the member offline.
    /// Called on guild-stage enter and on disconnect.
    /// </summary>
    Task<GuildResponse> UpdateChannel(GuildUpdateChannelRequest request);

    // ── BBS ──────────────────────────────────────────────────────────────

    /// <summary>Returns a paged list of BBS posts for the given guild.</summary>
    Task<GuildBBSLoadResponse> BBSLoad(GuildBBSLoadRequest request);

    /// <summary>Returns the full content of a single BBS post including comments.</summary>
    Task<GuildBBSViewResponse> BBSView(GuildBBSViewRequest request);

    /// <summary>Creates a new BBS post.</summary>
    Task<GuildBBSViewResponse> BBSWrite(GuildBBSWriteRequest request);

    /// <summary>Edits an existing BBS post (author or guild master).</summary>
    Task<GuildBBSViewResponse> BBSEdit(GuildBBSEditRequest request);

    /// <summary>Deletes a BBS post and all its comments (author or guild master).</summary>
    Task<GuildResponse> BBSDelete(GuildBBSDeleteRequest request);

    /// <summary>Adds a comment to an existing BBS post.</summary>
    Task<GuildBBSViewResponse> BBSWriteComment(GuildBBSWriteCommentRequest request);

    /// <summary>Deletes a single comment (comment author or guild master).</summary>
    Task<GuildBBSViewResponse> BBSDeleteComment(GuildBBSDeleteCommentRequest request);
}
