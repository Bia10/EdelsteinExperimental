using Edelstein.Protocol.Gameplay.Game.Objects.User;

namespace Edelstein.Protocol.Gameplay.Game.Contracts;

public record FieldOnPacketGuildSetMarkRequest(
    IFieldUser User,
    short MarkBg,
    byte MarkBgColor,
    short Mark,
    byte MarkColor
);
