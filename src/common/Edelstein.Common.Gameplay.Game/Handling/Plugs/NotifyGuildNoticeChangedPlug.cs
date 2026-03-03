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
/// Broadcasts the new guild notice text to all online guild members.
/// Wire: LP_GuildResult GuildRes_SetNotice_Done (0x47).
/// Payload: guildID(4) + notice(str).
/// </summary>
public class NotifyGuildNoticeChangedPlug : IPipelinePlug<NotifyGuildNoticeChanged>
{
    private readonly IGameStage _stage;

    public NotifyGuildNoticeChangedPlug(IGameStage stage) =>
        _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildNoticeChanged message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users
            .Where(u => u.Guild?.ID == message.GuildID)
            .ToImmutableArray();

        foreach (var user in affected)
        {
            // Keep the cached notice in sync so rate-modifier lookups stay accurate.
            if (user.Guild is GuildMembership ms)
                ms.Notice = message.Notice;

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.SetNotice_Done);
            packet.WriteInt(message.GuildID);
            packet.WriteString(message.Notice);
            _ = user.Dispatch(packet.Build());
        }
    }
}
