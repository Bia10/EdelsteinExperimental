namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when a member's character level or job changes.
/// Wire: LP_GuildResult GuildRes_ChangeLevelOrJob (0x3E).
/// Payload: guildID(4) + charID(4) + level(4) + job(4).
/// </summary>
public record NotifyGuildMemberLevelOrJobChanged(int GuildID, int CharacterID, int Level, int Job);
