using System.Collections.Immutable;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Services.Social;
using Edelstein.Protocol.Utilities.Packets;

namespace Edelstein.Common.Gameplay.Social;

public static class GuildExtensions
{
    /// <summary>
    /// Writes a single <c>GUILDMEMBER</c> raw struct (exactly 37 bytes).
    /// Layout: sCharacterName[13] + nJob(4) + nLevel(4) + nGrade(4) +
    ///         bOnLine(4) + nCommitment(4) + nAllianceGrade(4).
    /// </summary>
    public static IPacketWriter WriteGuildMember(this IPacketWriter writer, IGuildMember member)
    {
        writer.WriteString(member.CharacterName, 13);
        writer.WriteInt(member.Job);
        writer.WriteInt(member.Level);
        writer.WriteInt(member.Grade);
        writer.WriteInt(member.ChannelID >= 0 ? 1 : 0);
        writer.WriteInt(member.Commitment);
        writer.WriteInt(member.AllianceGrade);
        return writer;
    }

    /// <summary>
    /// Writes a single <c>GUILDDATA::SKILLENTRY</c> block.
    /// Layout: nLevel(2) + dateExpire(8 raw FILETIME) + strBuyCharacterName(str).
    /// </summary>
    public static IPacketWriter WriteGuildSkillEntry(this IPacketWriter writer, IGuildSkillRecord skill)
    {
        writer.WriteShort((short)skill.Level);
        writer.WriteDateTime(skill.DateExpire);
        writer.WriteString(skill.BuyerName);
        return writer;
    }

    /// <summary>
    /// Writes the complete <c>GUILDDATA</c> payload that the client decodes
    /// via <c>GUILDDATA::Decode</c>.
    /// </summary>
    /// <remarks>
    /// The master member (grade = 1) is always written first in both the
    /// character-ID bulk array and the GUILDMEMBER bulk array, which is
    /// required by <c>CWvsContext::AmIGuildMaster</c> that reads
    /// <c>adwCharacterID[0]</c> to identify the master.
    /// </remarks>
    public static IPacketWriter WriteGuildData(this IPacketWriter writer, IGuildMembership membership)
    {
        // ── Guild header ──────────────────────────────────────────────────────
        writer.WriteInt(membership.ID);
        writer.WriteString(membership.Name);

        // 5 grade name strings (always write all 5, empty if unused).
        writer.WriteString(membership.GradeName1);
        writer.WriteString(membership.GradeName2);
        writer.WriteString(membership.GradeName3);
        writer.WriteString(membership.GradeName4);
        writer.WriteString(membership.GradeName5);

        // ── Member arrays (master first, then rest sorted by grade then charID) ─
        var members = membership.Members.Values
            .OrderBy(m => m.Grade)
            .ThenBy(m => m.CharacterID)
            .ToImmutableList();

        writer.WriteByte((byte)members.Count);

        // Two separate bulk buffers: all character IDs first, then all member data.
        foreach (var m in members)
            writer.WriteInt(m.CharacterID);

        foreach (var m in members)
            writer.WriteGuildMember(m);

        // ── Guild settings ────────────────────────────────────────────────────
        writer.WriteInt(membership.MaxMemberNum);

        writer.WriteShort(membership.MarkBg);
        writer.WriteByte(membership.MarkBgColor);
        writer.WriteShort(membership.Mark);
        writer.WriteByte(membership.MarkColor);

        writer.WriteString(membership.Notice);
        writer.WriteInt(membership.Point);
        writer.WriteInt(membership.AllianceID);
        writer.WriteByte(membership.GuildLevel);

        // ── Guild skill records ───────────────────────────────────────────────
        var skills = membership.Skills.Values.ToImmutableList();

        writer.WriteShort((short)skills.Count);
        foreach (var skill in skills)
        {
            writer.WriteInt(skill.SkillID);
            writer.WriteGuildSkillEntry(skill);
        }

        return writer;
    }
}
