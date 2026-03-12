using Edelstein.Common.Services.Social.Entities;
using Edelstein.Protocol.Services.Social;

namespace Edelstein.Common.Services.Social;

/// <summary>
/// Immutable snapshot of a BBS post, built from a <see cref="GuildBBSPostEntity"/>.
/// </summary>
public class GuildBBSPost : IGuildBBSPost
{
    public int ID { get; }
    public int GuildID { get; }
    public int AuthorID { get; }
    public string AuthorName { get; }
    public bool IsNotice { get; }
    public string Title { get; }
    public string Content { get; }
    public DateTime CreatedAt { get; }
    public int EmoticonID { get; }
    public int CommentCount { get; }

    public GuildBBSPost(GuildBBSPostEntity entity)
    {
        ID = entity.ID;
        GuildID = entity.GuildID;
        AuthorID = entity.AuthorID;
        AuthorName = entity.AuthorName;
        IsNotice = entity.IsNotice;
        Title = entity.Title;
        Content = entity.Content;
        CreatedAt = entity.CreatedAt;
        EmoticonID = entity.EmoticonID;
        CommentCount = entity.CommentCount;
    }
}
