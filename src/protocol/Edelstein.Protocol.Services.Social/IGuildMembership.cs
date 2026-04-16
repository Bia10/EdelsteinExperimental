namespace Edelstein.Protocol.Services.Social;

/// <summary>
/// The full in-memory snapshot of a user's guild, maintained once for each
/// logged-in character that belongs to a guild.  It combines guild-header data
/// (<see cref="IGuild"/>), the current user's own member record
/// (<see cref="IGuildMember"/>), the complete member roster, and the guild's
/// purchased skills—exactly the information encoded in <c>GUILDDATA::Decode</c>.
/// </summary>
public interface IGuildMembership : IGuild, IGuildMember
{
    /// <summary>
    /// All guild members keyed by <see cref="IGuildMember.CharacterID"/>.
    /// Includes the current user's own entry.
    /// </summary>
    IDictionary<int, IGuildMember> Members { get; }

    /// <summary>
    /// All purchased guild skills keyed by <see cref="IGuildSkillRecord.SkillID"/>.
    /// </summary>
    IDictionary<int, IGuildSkillRecord> Skills { get; }
}
