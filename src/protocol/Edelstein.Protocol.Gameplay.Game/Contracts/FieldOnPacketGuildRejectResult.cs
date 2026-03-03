using Edelstein.Protocol.Gameplay.Game.Objects.User;

namespace Edelstein.Protocol.Gameplay.Game.Contracts;

/// <summary>
/// Raised when a player declines or is already-invited for a guild invitation.
/// Wire: CP_GuildResult 0x37 (decline) or 0x38 (already invited).
/// Payload: EncodeStr(inviterName) + EncodeStr(myName).
/// </summary>
public record FieldOnPacketGuildRejectResult(
    IFieldUser User,
    string InviterName,
    bool IsAlreadyInvited
);
