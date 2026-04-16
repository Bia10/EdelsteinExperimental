using System.Collections.Immutable;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Services.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Broadcasts a guild skill purchase or upgrade to all online guild members.
/// Wire: LP_GuildResult GuildRes_SetSkill_Done (0x51).
/// Payload: guildID(4) + skillID(4) + SKILLENTRY (level(2) + dateExpire(8) + buyerName(str)).
/// </summary>
public class NotifyGuildSkillUpdatedPlug : IPipelinePlug<NotifyGuildSkillUpdated>
{
    private readonly IGameStage _stage;

    public NotifyGuildSkillUpdatedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildSkillUpdated message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users.Where(u => u.Guild?.ID == message.GuildID).ToImmutableArray();

        foreach (var user in affected)
        {
            // Insert or update the skill entry in the local snapshot.
            if (user.Guild != null)
                user.Guild.Skills[message.Skill.SkillID] = new GuildMembershipSkill(message.Skill);

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.SetSkill_Done);
            packet.WriteInt(message.GuildID);
            packet.WriteInt(message.Skill.SkillID);
            packet.WriteGuildSkillEntry(message.Skill);
            _ = user.Dispatch(packet.Build());
        }
    }
}
