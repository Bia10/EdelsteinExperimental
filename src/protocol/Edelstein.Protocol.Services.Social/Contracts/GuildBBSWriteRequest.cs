namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Creates a new BBS post in the guild board.</summary>
public record GuildBBSWriteRequest(
    int GuildID,
    int AuthorID,
    string AuthorName,
    bool IsNotice,
    string Title,
    string Content,
    int EmoticonID
);
