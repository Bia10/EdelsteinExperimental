namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildCreateRequest(
    int CharacterID,
    string CharacterName,
    int Job,
    int Level,
    int ChannelID,
    int FieldID,
    string GuildName
);
