using Edelstein.Common.Services.Social.Entities;
using Edelstein.Protocol.Services.Social;

namespace Edelstein.Common.Services.Social;

/// <summary>
/// In-memory snapshot of a logged-in character's complete guild state,
/// built from the EF Core entities at login time and kept current by
/// message-bus notifications as other members make changes.
/// </summary>
public class GuildMembership : IGuildMembership
{

    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GradeName1 { get; set; } = string.Empty;
    public string GradeName2 { get; set; } = string.Empty;
    public string GradeName3 { get; set; } = string.Empty;
    public string GradeName4 { get; set; } = string.Empty;
    public string GradeName5 { get; set; } = string.Empty;
    public int MaxMemberNum { get; set; }
    public int MasterCharacterID { get; set; }
    public short MarkBg { get; set; }
    public byte MarkBgColor { get; set; }
    public short Mark { get; set; }
    public byte MarkColor { get; set; }
    public string Notice { get; set; } = string.Empty;
    public int Point { get; set; }
    public byte GuildLevel { get; set; }
    public int AllianceID { get; set; }


    public int GuildID { get; set; }
    public int CharacterID { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int Job { get; set; }
    public int Level { get; set; }
    public int Grade { get; set; }
    public int ChannelID { get; set; }
    public int Commitment { get; set; }
    public int AllianceGrade { get; set; }

    public IDictionary<int, IGuildMember> Members { get; set; } = new Dictionary<int, IGuildMember>();
    public IDictionary<int, IGuildSkillRecord> Skills { get; set; } = new Dictionary<int, IGuildSkillRecord>();

    public GuildMembership() { }

    /// <summary>
    /// Constructs the snapshot from the <paramref name="guildMember"/> EF entity,
    /// which must have its <c>Guild.Members</c> and <c>Guild.Skills</c>
    /// navigation properties loaded.
    /// </summary>
    public GuildMembership(GuildMemberEntity guildMember)
    {
        var guild = guildMember.Guild;


        ID = guild.ID;
        Name = guild.Name;
        GradeName1 = guild.GradeName1;
        GradeName2 = guild.GradeName2;
        GradeName3 = guild.GradeName3;
        GradeName4 = guild.GradeName4;
        GradeName5 = guild.GradeName5;
        MaxMemberNum = guild.MaxMemberNum;
        MasterCharacterID = guild.MasterCharacterID;
        MarkBg = guild.MarkBg;
        MarkBgColor = guild.MarkBgColor;
        Mark = guild.Mark;
        MarkColor = guild.MarkColor;
        Notice = guild.Notice;
        Point = guild.Point;
        GuildLevel = guild.GuildLevel;
        AllianceID = guild.AllianceID;


        GuildID = guildMember.GuildID;
        CharacterID = guildMember.CharacterID;
        CharacterName = guildMember.CharacterName;
        Job = guildMember.Job;
        Level = guildMember.Level;
        Grade = guildMember.Grade;
        ChannelID = guildMember.ChannelID;
        Commitment = guildMember.Commitment;
        AllianceGrade = guildMember.AllianceGrade;


        Members = guild.Members
            .ToDictionary(
                m => m.CharacterID,
                m => (IGuildMember)new GuildMembershipMember(m));


        Skills = guild.Skills
            .ToDictionary(
                s => s.SkillID,
                s => (IGuildSkillRecord)new GuildMembershipSkill(s));
    }
}
