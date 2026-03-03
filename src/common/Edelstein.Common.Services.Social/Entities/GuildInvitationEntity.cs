using Edelstein.Protocol.Utilities.Repositories;

namespace Edelstein.Common.Services.Social.Entities;

public record GuildInvitationEntity : IIdentifiable<int>
{
    public int ID { get; set; }

    public int GuildID { get; set; }
    public GuildEntity Guild { get; set; } = null!;

    /// <summary>Character ID of the inviter; allows notifying them on reject.</summary>
    public int InviterID { get; set; }

    /// <summary>Character ID of the invitee.</summary>
    public int CharacterID { get; set; }

    /// <summary>UTC timestamp after which this invitation expires (default: 3 minutes).</summary>
    public DateTime DateExpire { get; set; }
}
