using Edelstein.Protocol.Services.Social;
using Edelstein.Protocol.Utilities.Repositories;

namespace Edelstein.Common.Services.Social.Entities;

public class GuildBBSCommentEntity : IGuildBBSComment, IIdentifiable<int>
{
    public int ID { get; set; }

    public int PostID { get; set; }

    public int GuildID { get; set; }

    public int AuthorID { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public GuildBBSPostEntity Post { get; set; } = null!;
}
