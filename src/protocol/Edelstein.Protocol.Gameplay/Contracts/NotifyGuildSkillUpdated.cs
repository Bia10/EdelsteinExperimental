using Edelstein.Protocol.Services.Social;

namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when a guild skill entry is purchased or upgraded.
/// Wire: LP_GuildResult GuildRes_SetSkill_Done (0x51).
/// Payload: guildID(4) + skillID(4) + SKILLENTRY (level(2) + dateExpire(8) + buyerName(str)).
/// </summary>
public record NotifyGuildSkillUpdated(int GuildID, IGuildSkillRecord Skill);
