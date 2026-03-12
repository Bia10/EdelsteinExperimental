namespace Edelstein.Protocol.Services.Social;

/// <summary>
/// Represents a single guild BBS post, corresponding to one row in the
/// <c>guild_bbs_posts</c> table.
/// </summary>
public interface IGuildBBSPost
{
    /// <summary>Unique article identifier (the client calls this <c>nEntryID</c> / <c>nCurEntryID</c>).</summary>
    int ID { get; }

    /// <summary>Guild this post belongs to.</summary>
    int GuildID { get; }

    /// <summary>Character ID of the post author (<c>nCharacterID</c> / <c>nCurCharacterID</c>).</summary>
    int AuthorID { get; }

    /// <summary>Display name of the post author at time of posting.</summary>
    string AuthorName { get; }

    /// <summary>Whether the post is pinned as the guild notice. Only one notice per guild is allowed.</summary>
    bool IsNotice { get; }

    /// <summary>Post subject / title (<c>sTitle</c> / <c>sCurTitle</c>).</summary>
    string Title { get; }

    /// <summary>Full post body (<c>sCurText</c>).</summary>
    string Content { get; }

    /// <summary>UTC timestamp of when the post was created (<c>ftDate</c> / <c>ftCurDate</c>, encoded as Windows FILETIME).</summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Emoticon ID attached to the post (<c>nEmoticon</c>).
    /// 0 means no emoticon.
    /// </summary>
    int EmoticonID { get; }

    /// <summary>Total number of comments on this post (<c>nComments</c>).</summary>
    int CommentCount { get; }
}
