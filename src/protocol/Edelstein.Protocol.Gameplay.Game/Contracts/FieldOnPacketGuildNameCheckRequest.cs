using Edelstein.Protocol.Gameplay.Game.Objects.User;

namespace Edelstein.Protocol.Gameplay.Game.Contracts;

/// <summary>
/// Raised when the client sends CP_GuildRequest 0x02 (CheckGuildName) to
/// validate a proposed guild name before committing to creation.
/// Wire: Encode1(2) + EncodeStr(guildName).
/// </summary>
public record FieldOnPacketGuildNameCheckRequest(IFieldUser User, string GuildName);
