using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles the <c>NotifyGuildInviteRejected</c> message-bus event.
/// Finds the original inviter and sends <c>InviteGuild_Rejected (0x39)</c>
/// with the rejecter's name so the inviter sees the declination chat message.
/// Wire layout (V95 §11 case 57): rejecterName(str).
/// </summary>
public class NotifyGuildInviteRejectedPlug : IPipelinePlug<NotifyGuildInviteRejected>
{
    private readonly IGameStage _stage;

    public NotifyGuildInviteRejectedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildInviteRejected message)
    {
        var inviter = await _stage.Users.Retrieve(message.InviterID);
        if (inviter == null) return;

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        // 0x38 (AlreadyInvited) → inviter sees chat 0xACF "%s refused" (target was already in guild)
        // 0x39 (Rejected)       → inviter sees chat 0x15C "%s denied" (explicit decline)
        packet.WriteByte(message.IsAlreadyInvited
            ? (byte)GuildResultOperations.InviteGuild_AlreadyInvited
            : (byte)GuildResultOperations.InviteGuild_Rejected);
        packet.WriteString(message.RejecterName);
        _ = inviter.Dispatch(packet.Build());
    }
}
