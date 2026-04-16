using Edelstein.Protocol.Gameplay.Game.Objects.User;

namespace Edelstein.Protocol.Gameplay.Game.Contracts;

/// <summary>
/// Raised when a player accepts a pending guild invitation
/// (CP_GuildRequest byte 0x06 = JoinGuild).
/// Wire payload: inviterID(4) + myCharacterID(4) — client sends the inviterID,
/// not the guildID; the service looks up the pending invitation by inviterID.
/// </summary>
public record FieldOnPacketGuildJoinRequest(IFieldUser User, int InviterID);
