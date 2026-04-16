using Edelstein.Protocol.Gameplay.Game.Objects.User;

namespace Edelstein.Protocol.Gameplay.Game.Contracts;

public record FieldOnPacketGuildKickRequest(
    IFieldUser User,
    int TargetCharacterID,
    string TargetCharacterName
);
