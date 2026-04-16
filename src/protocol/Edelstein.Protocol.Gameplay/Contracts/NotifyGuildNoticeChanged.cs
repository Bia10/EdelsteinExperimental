namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when the guild notice text is updated.
/// Wire: LP_GuildResult GuildRes_SetNotice_Done (0x47).
/// Payload: guildID(4) + notice(str).
/// </summary>
public record NotifyGuildNoticeChanged(int GuildID, string Notice);
