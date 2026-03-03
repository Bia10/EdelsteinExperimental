using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles a master kicking a member from the guild (KickGuild, 0x08).
/// On success <c>NotifyGuildMemberWithdrawn</c> broadcasts the expulsion
/// with <c>IsKicked = true</c>, which maps to <c>KickGuild_Done (0x31)</c>.
/// </summary>
public class FieldOnPacketGuildKickRequestPlug : IPipelinePlug<FieldOnPacketGuildKickRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildKickRequest message)
    {
        if (message.User.StageUser.Guild == null)
            return;

        var response = await message.User.StageUser.Context.Services.Guild.Kick(
            new GuildKickRequest(
                message.User.StageUser.Guild.ID,
                message.User.Character.ID,        // masterID
                message.TargetCharacterID,
                message.TargetCharacterName
            ));

        // Success → NotifyGuildMemberWithdrawn (isKicked=true) handles broadcast.
        if (response.Result == GuildResult.Success)
            return;

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)GuildResultOperations.KickGuild_Unknown);
        await message.User.Dispatch(packet.Build());
    }
}
