using System.Collections.Immutable;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles the <c>NotifyGuildDisbanded</c> message-bus event.
/// Clears each online guild member's <see cref="IGameStageUser.Guild"/> state
/// and sends <c>RemoveGuild_Done (0x34)</c> to them.
/// </summary>
public class NotifyGuildDisbandedPlug : IPipelinePlug<NotifyGuildDisbanded>
{
    private readonly IGameStage _stage;

    public NotifyGuildDisbandedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildDisbanded message)
    {
        var users = await _stage.Users.RetrieveAll();
        var guildMembers = users.Where(u => u.Guild?.ID == message.GuildID).ToImmutableArray();

        foreach (var user in guildMembers)
        {
            user.Guild = null;

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.RemoveGuild_Done);
            // R-002: payload is guildID(4) only. Client reads it as a guard;
            // if it does not match own guildID the packet is silently ignored.
            packet.WriteInt(message.GuildID);
            _ = user.Dispatch(packet.Build());
        }
    }
}
