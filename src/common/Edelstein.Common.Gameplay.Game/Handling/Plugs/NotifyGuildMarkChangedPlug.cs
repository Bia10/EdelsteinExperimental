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
/// Broadcasts the guild emblem (mark) update to all online guild members.
/// Wire: LP_GuildResult GuildRes_SetMark_Done (0x45).
/// Payload: guildID(4) + markBg(2) + markBgColor(1) + mark(2) + markColor(1).
/// </summary>
public class NotifyGuildMarkChangedPlug : IPipelinePlug<NotifyGuildMarkChanged>
{
    private readonly IGameStage _stage;

    public NotifyGuildMarkChangedPlug(IGameStage stage) =>
        _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildMarkChanged message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users
            .Where(u => u.Guild?.ID == message.GuildID)
            .ToImmutableArray();

        foreach (var user in affected)
        {
            // Persist the new mark into the local snapshot.
            if (user.Guild is GuildMembership ms)
            {
                ms.MarkBg = message.MarkBg;
                ms.MarkBgColor = message.MarkBgColor;
                ms.Mark = message.Mark;
                ms.MarkColor = message.MarkColor;
            }

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.SetMark_Done);
            packet.WriteInt(message.GuildID);
            packet.WriteShort(message.MarkBg);
            packet.WriteByte(message.MarkBgColor);
            packet.WriteShort(message.Mark);
            packet.WriteByte(message.MarkColor);
            _ = user.Dispatch(packet.Build());
        }
    }
}
