namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildInviteAcceptRequest(
    int GuildID,
    int CharacterID,
    string CharacterName,
    int Job,
    int Level,
    int ChannelID,
    int FieldID
);
