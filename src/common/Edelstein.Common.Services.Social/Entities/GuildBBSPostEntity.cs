using Edelstein.Protocol.Services.Social;
using Edelstein.Protocol.Utilities.Repositories;

namespace Edelstein.Common.Services.Social.Entities;

public class GuildBBSPostEntity : IGuildBBSPost, IIdentifiable<int>
{
    public int ID { get; set; }

    public int GuildID { get; set; }

    public int AuthorID { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public bool IsNotice { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int EmoticonID { get; set; }

    public int CommentCount { get; set; }

    public GuildEntity Guild { get; set; } = null!;

    public ICollection<GuildBBSCommentEntity> Comments { get; set; } = new List<GuildBBSCommentEntity>();
}
