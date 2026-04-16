namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when the guild emblem (mark) is updated.
/// Wire: LP_GuildResult GuildRes_SetMark_Done (0x45).
/// Payload: guildID(4) + markBg(2) + markBgColor(1) + mark(2) + markColor(1).
/// </summary>
public record NotifyGuildMarkChanged(
    int GuildID,
    short MarkBg,
    byte MarkBgColor,
    short Mark,
    byte MarkColor
);
