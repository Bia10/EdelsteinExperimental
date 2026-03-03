using Edelstein.Protocol.Gameplay.Game.Objects.User;

namespace Edelstein.Protocol.Gameplay.Game.Contracts;

public record FieldOnPacketGuildSetGradeNamesRequest(
    IFieldUser User,
    string[] GradeNames
);
