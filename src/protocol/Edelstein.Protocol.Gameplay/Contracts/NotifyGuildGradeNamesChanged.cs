namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when the guild grade names are updated.
/// Wire: LP_GuildResult GuildRes_SetGradeName_Done (0x40).
/// Payload: guildID(4) + 5 grade name strings.
/// </summary>
public record NotifyGuildGradeNamesChanged(int GuildID, string[] GradeNames);
