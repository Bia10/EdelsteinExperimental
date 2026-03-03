using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Checks whether a proposed guild name is available.
/// Sends <c>CheckGuildName_Available (0x1D)</c> or
/// <c>CheckGuildName_AlreadyUsed (0x1E)</c> back to the requesting player.
/// </summary>
public class FieldOnPacketGuildNameCheckRequestPlug : IPipelinePlug<FieldOnPacketGuildNameCheckRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildNameCheckRequest message)
    {
        var response = await message.User.StageUser.Context.Services.Guild.CheckName(
            new GuildNameCheckRequest(message.GuildName));

        var opcode = response.Result switch
        {
            GuildResult.Success       => GuildResultOperations.CheckGuildName_Available,
            GuildResult.FailedNameTaken => GuildResultOperations.CheckGuildName_AlreadyUsed,
            _                         => GuildResultOperations.CheckGuildName_Unknown
        };

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)opcode);

        // On success the client needs the validated name to proceed with creation.
        if (opcode == GuildResultOperations.CheckGuildName_Available)
            packet.WriteString(message.GuildName);

        await message.User.Dispatch(packet.Build());
    }
}
