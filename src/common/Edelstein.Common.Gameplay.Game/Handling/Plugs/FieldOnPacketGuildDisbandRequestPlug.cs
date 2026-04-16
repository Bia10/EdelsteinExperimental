using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles guild dissolution (disband) requests from the guild master.
/// On success <c>NotifyGuildDisbanded</c> clears the guild state for all
/// online members and sends <c>RemoveGuild_Done (0x34)</c> to each of them.
/// </summary>
public class FieldOnPacketGuildDisbandRequestPlug : IPipelinePlug<FieldOnPacketGuildDisbandRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildDisbandRequest message)
    {
        if (message.User.StageUser.Guild == null)
            return;

        var response = await message.User.StageUser.Context.Services.Guild.Disband(
            new GuildDisbandRequest(message.User.StageUser.Guild.ID, message.User.Character.ID)
        );

        // Success → NotifyGuildDisbanded handles broadcast.
        if (response.Result == GuildResult.Success)
            return;

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)GuildResultOperations.RemoveGuild_Unknown);
        await message.User.Dispatch(packet.Build());
    }
}
