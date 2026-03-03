namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildSetGradeNamesRequest(
    int GuildID,
    int CharacterID,
    string GradeName1,
    string GradeName2,
    string GradeName3,
    string GradeName4,
    string GradeName5
);
