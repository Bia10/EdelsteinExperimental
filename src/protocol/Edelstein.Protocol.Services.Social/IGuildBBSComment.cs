namespace Edelstein.Protocol.Services.Social;

/// <summary>
/// Represents a single comment on a guild BBS post, corresponding to one row
/// in the <c>guild_bbs_comments</c> table.
/// </summary>
public interface IGuildBBSComment
{
    /// <summary>Unique comment identifier (the client calls this <c>bOid</c>).</summary>
    int ID { get; }

    /// <summary>ID of the parent post.</summary>
    int PostID { get; }

    /// <summary>Guild this comment's post belongs to.</summary>
    int GuildID { get; }

    /// <summary>Character ID of the commenter.</summary>
    int AuthorID { get; }

    /// <summary>Display name of the commenter at time of posting.</summary>
    string AuthorName { get; }

    /// <summary>Comment body text.</summary>
    string Content { get; }

    /// <summary>UTC timestamp of when this comment was posted.</summary>
    DateTime CreatedAt { get; }
}
