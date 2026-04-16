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
/// Broadcasts the guild member capacity increase to all online guild members.
/// Wire: LP_GuildResult GuildRes_IncMaxMemberNum_Done (0x3C).
/// Payload: guildID(4) + maxMemberNum(4).
/// </summary>
public class NotifyGuildMaxMemberChangedPlug : IPipelinePlug<NotifyGuildMaxMemberChanged>
{
    private readonly IGameStage _stage;

    public NotifyGuildMaxMemberChangedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildMaxMemberChanged message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users.Where(u => u.Guild?.ID == message.GuildID).ToImmutableArray();

        foreach (var user in affected)
        {
            if (user.Guild is GuildMembership ms)
                ms.MaxMemberNum = message.MaxMemberNum;

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.IncMaxMemberNum_Done);
            packet.WriteInt(message.GuildID);
            packet.WriteInt(message.MaxMemberNum);
            _ = user.Dispatch(packet.Build());
        }
    }
}
