using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles a player voluntarily leaving their guild (WithdrawGuild, 0x07).
/// The guild master cannot withdraw; they must disband first (RemoveGuild).
/// On success <c>NotifyGuildMemberWithdrawn</c> broadcasts the removal.
/// </summary>
public class FieldOnPacketGuildLeaveRequestPlug : IPipelinePlug<FieldOnPacketGuildLeaveRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildLeaveRequest message)
    {
        if (message.User.StageUser.Guild == null)
            return;

        var response = await message.User.StageUser.Context.Services.Guild.Leave(
            new GuildLeaveRequest(
                message.User.StageUser.Guild.ID,
                message.User.Character.ID,
                message.User.Character.Name
            )
        );

        // Success → NotifyGuildMemberWithdrawn handles broadcast.
        if (response.Result == GuildResult.Success)
            return;

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)GuildResultOperations.WithdrawGuild_Unknown);
        await message.User.Dispatch(packet.Build());
    }
}
