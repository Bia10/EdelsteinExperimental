using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles guild creation requests.
/// On failure the appropriate <c>CreateNewGuild_*</c> error opcode is returned
/// to the requesting player. On success the <c>NotifyGuildCreated</c> bus
/// message takes care of sending the full <c>GUILDDATA</c> to the creator.
/// </summary>
public class FieldOnPacketGuildCreateRequestPlug : IPipelinePlug<FieldOnPacketGuildCreateRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildCreateRequest message)
    {
        var response = await message.User.StageUser.Context.Services.Guild.Create(
            new GuildCreateRequest(
                message.User.Character.ID,
                message.User.Character.Name,
                message.User.Character.Job,
                message.User.Character.Level,
                message.User.StageUser.Context.Options.ChannelID,
                message.User.Field?.ID ?? 999999999,
                message.GuildName
            ));

        // Success → NotifyGuildCreated broadcast handles the full client update.
        if (response.Result == GuildResult.Success)
            return;

        var opcode = response.Result switch
        {
            GuildResult.FailedAlreadyInGuild => GuildResultOperations.CreateNewGuild_AlreadyJoined,
            GuildResult.FailedNameTaken      => GuildResultOperations.CreateNewGuild_GuildNameAlreadyExist,
            GuildResult.FailedBeginner       => GuildResultOperations.CreateNewGuild_Beginner,
            _                                => GuildResultOperations.CreateNewGuild_Unknown
        };

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)opcode);
        await message.User.Dispatch(packet.Build());
    }
}
