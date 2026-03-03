using Edelstein.Protocol.Gameplay.Game.Objects.User;

namespace Edelstein.Protocol.Gameplay.Game.Contracts;

public record FieldOnPacketGuildSetMemberGradeRequest(
    IFieldUser User,
    int TargetCharacterID,
    int Grade
);
