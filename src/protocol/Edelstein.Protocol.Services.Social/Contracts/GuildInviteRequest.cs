namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildInviteRequest(
    int InviterID,
    string InviterName,
    int GuildID,
    string CharacterName
);
