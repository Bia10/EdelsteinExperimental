namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when an invited character explicitly declines a guild invitation.
/// Routes a notification to the original inviter's game server instance.
/// Wire to inviter: LP_GuildResult GuildRes_InviteGuild_Rejected (0x39).
/// Payload to inviter: rejecterName(str).
/// </summary>
public record NotifyGuildInviteRejected(
    int InviterID,
    string RejecterName
);
