using Edelstein.Protocol.Gameplay.Game.Objects.User;

namespace Edelstein.Protocol.Gameplay.Game.Contracts;

/// <summary>
/// Raised when a player accepts a pending guild invitation
/// (CP_GuildRequest byte 0x06 = JoinGuild).
/// </summary>
public record FieldOnPacketGuildJoinRequest(
    IFieldUser User,
    int GuildID
);
