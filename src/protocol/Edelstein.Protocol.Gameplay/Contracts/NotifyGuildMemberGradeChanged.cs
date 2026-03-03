namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when a member's guild grade tier is changed.
/// Wire: LP_GuildResult GuildRes_SetMemberGrade_Done (0x42).
/// Payload: guildID(4) + charID(4) + grade(1).
/// </summary>
public record NotifyGuildMemberGradeChanged(
    int GuildID,
    int CharacterID,
    int Grade
);
