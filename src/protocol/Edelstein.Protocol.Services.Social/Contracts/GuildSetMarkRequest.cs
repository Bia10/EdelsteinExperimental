namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildSetMarkRequest(
    int GuildID,
    int CharacterID,
    short MarkBg,
    byte MarkBgColor,
    short Mark,
    byte MarkColor
);
