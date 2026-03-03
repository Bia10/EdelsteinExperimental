using Edelstein.Protocol.Services.Social;
using Edelstein.Protocol.Utilities.Repositories;

namespace Edelstein.Common.Services.Social.Entities;

public class GuildEntity : IGuild, IIdentifiable<int>
{
    public int ID { get; set; }

    public string Name { get; set; } = string.Empty;

    public string GradeName1 { get; set; } = string.Empty;
    public string GradeName2 { get; set; } = string.Empty;
    public string GradeName3 { get; set; } = string.Empty;
    public string GradeName4 { get; set; } = string.Empty;
    public string GradeName5 { get; set; } = string.Empty;

    public int MaxMemberNum { get; set; } = 10;

    public int MasterCharacterID { get; set; }

    public short MarkBg { get; set; }
    public byte MarkBgColor { get; set; }
    public short Mark { get; set; }
    public byte MarkColor { get; set; }

    public string Notice { get; set; } = string.Empty;

    /// <summary>Guild points (GP) earned via quests and missions.</summary>
    public int Point { get; set; }

    public byte GuildLevel { get; set; }

    public int AllianceID { get; set; }

    public ICollection<GuildMemberEntity> Members { get; set; } = new List<GuildMemberEntity>();
    public ICollection<GuildSkillEntity> Skills { get; set; } = new List<GuildSkillEntity>();
    public ICollection<GuildInvitationEntity> Invitations { get; set; } = new List<GuildInvitationEntity>();
}
