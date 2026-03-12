using Edelstein.Common.Services.Social.Entities;
using Edelstein.Protocol.Services.Social;

namespace Edelstein.Common.Services.Social;

/// <summary>
/// Immutable snapshot of a BBS comment, built from a <see cref="GuildBBSCommentEntity"/>.
/// </summary>
public class GuildBBSComment : IGuildBBSComment
{
    public int ID { get; }
    public int PostID { get; }
    public int GuildID { get; }
    public int AuthorID { get; }
    public string AuthorName { get; }
    public string Content { get; }
    public DateTime CreatedAt { get; }

    public GuildBBSComment(GuildBBSCommentEntity entity)
    {
        ID = entity.ID;
        PostID = entity.PostID;
        GuildID = entity.GuildID;
        AuthorID = entity.AuthorID;
        AuthorName = entity.AuthorName;
        Content = entity.Content;
        CreatedAt = entity.CreatedAt;
    }
}
