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
/// Broadcasts a member's level or job change to all online guild members.
/// Wire: LP_GuildResult GuildRes_ChangeLevelOrJob (0x3E).
/// Payload: guildID(4) + charID(4) + level(4) + job(4).
/// </summary>
public class NotifyGuildMemberLevelOrJobChangedPlug : IPipelinePlug<NotifyGuildMemberLevelOrJobChanged>
{
    private readonly IGameStage _stage;

    public NotifyGuildMemberLevelOrJobChangedPlug(IGameStage stage) =>
        _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildMemberLevelOrJobChanged message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users
            .Where(u => u.Guild?.ID == message.GuildID)
            .ToImmutableArray();

        foreach (var user in affected)
        {
            // Keep the local snapshot current so subsequent packets are coherent.
            if (user.Guild?.Members.TryGetValue(message.CharacterID, out var member) == true
                && member is GuildMembershipMember m)
            {
                m.Level = message.Level;
                m.Job = message.Job;
            }

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.ChangeLevelOrJob);
            packet.WriteInt(message.GuildID);
            packet.WriteInt(message.CharacterID);
            packet.WriteInt(message.Level);
            packet.WriteInt(message.Job);
            _ = user.Dispatch(packet.Build());
        }
    }
}
