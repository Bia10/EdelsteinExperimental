namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildKickRequest(
    int GuildID,
    int MasterID,
    int CharacterID,
    string CharacterName
);
