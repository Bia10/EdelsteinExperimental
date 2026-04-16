namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when a member's online/offline status changes.
/// Wire: LP_GuildResult GuildRes_NotifyLoginOrLogout (0x3F).
/// Payload: guildID(4) + charID(4) + bOnLine(1).
/// </summary>
public record NotifyGuildMemberOnlineChanged(int GuildID, int CharacterID, bool IsOnline);
