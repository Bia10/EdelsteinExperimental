namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildSetMemberGradeRequest(
    int GuildID,
    int MasterID,
    int CharacterID,
    int Grade
);
