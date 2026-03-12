namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Edits an existing BBS post (author or master may edit).</summary>
public record GuildBBSEditRequest(
    int GuildID,
    int PostID,
    int RequesterID,
    bool IsNotice,
    string Title,
    string Content,
    int EmoticonID
);
