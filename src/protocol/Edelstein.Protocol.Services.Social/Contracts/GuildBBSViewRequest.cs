namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Requests the full content of a single BBS post including its comments.</summary>
public record GuildBBSViewRequest(int GuildID, int PostID);
