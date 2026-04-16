namespace Edelstein.Protocol.Services.Social;

/// <summary>
/// Per-member data stored in the guild roster, corresponding to a single row in
/// the <c>guild_members</c> table.
/// </summary>
public interface IGuildMember
{
    int GuildID { get; }

    int CharacterID { get; }
    string CharacterName { get; }

    int Job { get; }
    int Level { get; }

    /// <summary>Grade tier: 1 = Master, 2 = Jr. Master, 3–5 = regular tiers.</summary>
    int Grade { get; }

    /// <summary>Effective channel; ≥ 0 = online on that channel, −2 = offline.</summary>
    int ChannelID { get; }

    /// <summary>Cumulative guild-contribution points (nCommitment).</summary>
    int Commitment { get; }

    /// <summary>Alliance grade; 0 when the guild has no alliance.</summary>
    int AllianceGrade { get; }
}
