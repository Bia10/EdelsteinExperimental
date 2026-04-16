namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Deletes a comment from a BBS post (comment author or guild master).</summary>
public record GuildBBSDeleteCommentRequest(int GuildID, int PostID, int CommentID, int RequesterID);
