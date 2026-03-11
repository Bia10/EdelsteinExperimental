namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when an invited character declines a guild invitation.
/// Routes a notification to the original inviter's game server instance.
/// <list type="bullet">
///   <item><term>IsAlreadyInvited = false</term><description>Explicit decline: inviter receives <c>InviteGuild_Rejected (0x39)</c> — chat 0x15C.</description></item>
///   <item><term>IsAlreadyInvited = true</term><description>Target already in guild: inviter receives <c>InviteGuild_AlreadyInvited (0x38)</c> — chat 0xACF.</description></item>
/// </list>
/// </summary>
public record NotifyGuildInviteRejected(
    int InviterID,
    string RejecterName,
    bool IsAlreadyInvited = false
);
