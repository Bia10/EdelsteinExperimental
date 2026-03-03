using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles guild invitation requests.
/// Sends the inviter an acknowledgement:
/// <list type="bullet">
///   <item><c>InviteGuild_BlockedUser (0x37)</c> — invite successfully queued (chat 0x15B).</item>
///   <item><c>InviteGuild_AlreadyInvited (0x38)</c> — target already has a pending invite (chat 0xACF).</item>
/// </list>
/// When the service returns <c>Success</c> the <c>NotifyGuildMemberInvited</c> bus message
/// delivers the invitation popup to the target character.
/// </summary>
public class FieldOnPacketGuildInviteRequestPlug : IPipelinePlug<FieldOnPacketGuildInviteRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildInviteRequest message)
    {
        if (message.User.StageUser.Guild == null)
            return;

        var response = await message.User.StageUser.Context.Services.Guild.Invite(
            new GuildInviteRequest(
                message.User.Character.ID,
                message.User.Character.Name,
                message.User.StageUser.Guild.ID,
                message.CharacterName
            ));

        // Map service result to the LP_GuildResult acknowledgement opcode.
        var opcode = response.Result switch
        {
            GuildResult.Success => GuildResultOperations.InviteGuild_BlockedUser,  // "invite sent"
            GuildResult.FailedAlreadyInvited => GuildResultOperations.InviteGuild_AlreadyInvited,
            _ => GuildResultOperations.InviteGuild_AlreadyInvited
        };

        // Send acknowledgement to the inviter (contains the target character name).
        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)opcode);
        packet.WriteString(message.CharacterName);
        await message.User.Dispatch(packet.Build());
    }
}
