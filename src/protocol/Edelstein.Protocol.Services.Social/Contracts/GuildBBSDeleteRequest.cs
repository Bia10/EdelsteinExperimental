namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Deletes a BBS post (author or guild master may delete).</summary>
public record GuildBBSDeleteRequest(
    int GuildID,
    int PostID,
    int RequesterID
);
