using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles a player accepting a pending guild invitation (JoinGuild, 0x06).
/// On success <c>NotifyGuildMemberJoined</c> carries the updated snapshot to
/// all online guild members and the newly joined character.
/// </summary>
public class FieldOnPacketGuildJoinRequestPlug : IPipelinePlug<FieldOnPacketGuildJoinRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildJoinRequest message)
    {
        var response = await message.User.StageUser.Context.Services.Guild.InviteAccept(
            new GuildInviteAcceptRequest(
                message.InviterID,
                message.User.Character.ID,
                message.User.Character.Name,
                message.User.Character.Job,
                message.User.Character.Level,
                message.User.StageUser.Context.Options.ChannelID,
                message.User.Field?.ID ?? 999999999
            )
        );

        // Success → NotifyGuildMemberJoined broadcast handles user state + packets.
        if (response.Result == GuildResult.Success)
            return;

        var opcode = response.Result switch
        {
            GuildResult.FailedFull => GuildResultOperations.JoinGuild_AlreadyFull,
            GuildResult.FailedAlreadyInGuild => GuildResultOperations.JoinGuild_AlreadyJoined,
            GuildResult.FailedCharacterNotFound => GuildResultOperations.JoinGuild_UnknownUser,
            _ => GuildResultOperations.JoinGuild_Unknown,
        };

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)opcode);
        await message.User.Dispatch(packet.Build());
    }
}
