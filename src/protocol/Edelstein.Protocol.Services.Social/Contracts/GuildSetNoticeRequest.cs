namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildSetNoticeRequest(
    int GuildID,
    int CharacterID,
    string Notice
);
