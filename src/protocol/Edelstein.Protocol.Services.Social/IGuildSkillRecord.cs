namespace Edelstein.Protocol.Services.Social;

/// <summary>
/// Represents a single purchased guild skill, corresponding to one entry in
/// the <c>guild_skills</c> table and the per-skill block of <c>GUILDDATA::Decode</c>.
/// </summary>
public interface IGuildSkillRecord
{
    /// <summary>Guild skill template ID (range 91 000 000–91 009 999).</summary>
    int SkillID { get; }

    /// <summary>Current purchased level of this skill; 0 is never stored (not purchased).</summary>
    int Level { get; }

    /// <summary>
    /// UTC timestamp after which this skill expires.
    /// <see cref="DateTime.MaxValue"/> indicates a permanent purchase.
    /// </summary>
    DateTime DateExpire { get; }

    /// <summary>Name of the character who last upgraded this skill.</summary>
    string BuyerName { get; }
}
