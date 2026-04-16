using Edelstein.Common.Services.Social.Entities;
using Edelstein.Protocol.Services.Social;

namespace Edelstein.Common.Services.Social;

/// <summary>
/// Snapshot record for a single guild member; held inside
/// <see cref="GuildMembership.Members"/> and updated in-place by
/// message-bus notifications.
/// </summary>
public record GuildMembershipMember : IGuildMember
{
    public int GuildID { get; set; }
    public int CharacterID { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int Job { get; set; }
    public int Level { get; set; }
    public int Grade { get; set; }
    public int ChannelID { get; set; }
    public int Commitment { get; set; }
    public int AllianceGrade { get; set; }

    public GuildMembershipMember() { }

    public GuildMembershipMember(IGuildMember member)
    {
        GuildID = member.GuildID;
        CharacterID = member.CharacterID;
        CharacterName = member.CharacterName;
        Job = member.Job;
        Level = member.Level;
        Grade = member.Grade;
        ChannelID = member.ChannelID;
        Commitment = member.Commitment;
        AllianceGrade = member.AllianceGrade;
    }

    public GuildMembershipMember(GuildMemberEntity entity)
        : this((IGuildMember)entity) { }
}
