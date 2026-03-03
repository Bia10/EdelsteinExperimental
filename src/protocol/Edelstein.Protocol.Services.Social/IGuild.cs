using Edelstein.Protocol.Utilities.Repositories;

namespace Edelstein.Protocol.Services.Social;

/// <summary>
/// Persistent header data for a guild, as stored in the database.
/// Sub-interfaces (<see cref="IGuildMembership"/>) carry the full
/// runtime view including members and skills.
/// </summary>
public interface IGuild : IIdentifiable<int>
{
    string Name { get; }

    string GradeName1 { get; }
    string GradeName2 { get; }
    string GradeName3 { get; }
    string GradeName4 { get; }
    string GradeName5 { get; }

    int MaxMemberNum { get; }

    /// <summary>Character ID of the guild master (grade = 1).</summary>
    int MasterCharacterID { get; }

    short MarkBg { get; }
    byte MarkBgColor { get; }
    short Mark { get; }
    byte MarkColor { get; }

    string Notice { get; }

    /// <summary>Guild points accumulated via quests and missions (GP).</summary>
    int Point { get; }

    /// <summary>Guild level (0–5 in V95); governs skill-slot availability.</summary>
    byte GuildLevel { get; }

    /// <summary>Alliance this guild belongs to; 0 when the guild has no alliance.</summary>
    int AllianceID { get; }
}
