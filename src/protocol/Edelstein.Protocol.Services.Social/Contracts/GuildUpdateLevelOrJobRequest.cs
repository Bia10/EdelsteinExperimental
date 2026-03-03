namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildUpdateLevelOrJobRequest(
    int GuildID,
    int CharacterID,
    int Level,
    int Job
);
