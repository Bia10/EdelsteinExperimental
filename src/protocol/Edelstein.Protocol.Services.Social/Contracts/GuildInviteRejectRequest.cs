namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildInviteRejectRequest(
    int GuildID,
    int CharacterID,
    string CharacterName,
    bool IsAlreadyInvited = false
);
