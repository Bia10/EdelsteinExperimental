using Edelstein.Common.Services.Social.Entities;
using Edelstein.Protocol.Services.Social;

namespace Edelstein.Common.Services.Social;

/// <summary>
/// Snapshot record for a single purchased guild skill; held inside
/// <see cref="GuildMembership.Skills"/> and updated in-place when
/// <c>GuildRes_SetSkill_Done</c> is received.
/// </summary>
public record GuildMembershipSkill : IGuildSkillRecord
{
    public int SkillID { get; set; }
    public int Level { get; set; }
    public DateTime DateExpire { get; set; } = DateTime.MaxValue;
    public string BuyerName { get; set; } = string.Empty;

    public GuildMembershipSkill() { }

    public GuildMembershipSkill(IGuildSkillRecord skill)
    {
        SkillID = skill.SkillID;
        Level = skill.Level;
        DateExpire = skill.DateExpire;
        BuyerName = skill.BuyerName;
    }

    public GuildMembershipSkill(GuildSkillEntity entity)
        : this((IGuildSkillRecord)entity) { }
}
