namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Adds a comment to an existing BBS post.</summary>
public record GuildBBSWriteCommentRequest(
    int GuildID,
    int PostID,
    int AuthorID,
    string AuthorName,
    string Content
);
