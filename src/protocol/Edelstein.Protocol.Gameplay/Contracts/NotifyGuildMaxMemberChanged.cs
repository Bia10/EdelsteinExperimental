namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when the guild member capacity is expanded.
/// Wire: LP_GuildResult GuildRes_IncMaxMemberNum_Done (0x3C).
/// Payload: guildID(4) + maxMemberNum(4).
/// </summary>
public record NotifyGuildMaxMemberChanged(
    int GuildID,
    int MaxMemberNum
);
