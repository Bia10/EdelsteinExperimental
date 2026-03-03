using Edelstein.Protocol.Services.Social;
using Edelstein.Protocol.Utilities.Repositories;

namespace Edelstein.Common.Services.Social.Entities;

public record GuildMemberEntity : IGuildMember, IIdentifiable<int>
{
    public int ID { get; set; }

    public int GuildID { get; set; }
    public GuildEntity Guild { get; set; } = null!;

    public int CharacterID { get; set; }
    public string CharacterName { get; set; } = string.Empty;

    public int Job { get; set; }
    public int Level { get; set; }

    /// <summary>1 = Master, 2 = Jr. Master, 3–5 = member tiers.</summary>
    public int Grade { get; set; } = 5;

    /// <summary>Effective channel; −2 = offline.</summary>
    public int ChannelID { get; set; } = -2;

    /// <summary>Cumulative guild-contribution points (nCommitment).</summary>
    public int Commitment { get; set; }

    /// <summary>Alliance grade; 0 when the guild has no alliance.</summary>
    public int AllianceGrade { get; set; }
}
