using Edelstein.Protocol.Services.Social;
using Edelstein.Protocol.Utilities.Repositories;

namespace Edelstein.Common.Services.Social.Entities;

public record GuildSkillEntity : IGuildSkillRecord, IIdentifiable<int>
{
    public int ID { get; set; }

    public int GuildID { get; set; }
    public GuildEntity Guild { get; set; } = null!;

    /// <summary>Guild skill template ID (range 91 000 000–91 009 999).</summary>
    public int SkillID { get; set; }

    /// <summary>Current purchased level of this skill.</summary>
    public int Level { get; set; }

    /// <summary>
    /// UTC timestamp after which this skill expires.
    /// <see cref="DateTime.MaxValue"/> indicates a permanent purchase.
    /// </summary>
    public DateTime DateExpire { get; set; } = DateTime.MaxValue;

    /// <summary>Name of the character who last upgraded this skill.</summary>
    public string BuyerName { get; set; } = string.Empty;
}
