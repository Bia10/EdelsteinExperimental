using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Utilities.Packets;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles the <c>NotifyGuildMemberInvited</c> message-bus event.
/// Delivers the <c>GuildInvite (0x05)</c> invitation pop-up to the target
/// character with the inviter's name, job, level, and ID as decoded by the
/// client's <c>CUIFadeYesNo::CreateGuildInvite</c>.
/// Wire layout (V95 §11 case 5):
/// sub-opcode 0x05 + inviterName(str) + job(4) + level(4) + inviterID(4).
/// </summary>
public class NotifyGuildMemberInvitedPlug : IPipelinePlug<NotifyGuildMemberInvited>
{
    private readonly IGameStage _stage;

    public NotifyGuildMemberInvitedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildMemberInvited message)
    {
        var target = await _stage.Users.Retrieve(message.TargetCharacterID);
        if (target == null) return;

        // Inviter job/level are not persisted in the notification; look them up
        // from the stage. Fall back to 0 if the inviter went offline.
        var inviter      = await _stage.Users.Retrieve(message.InviterID);
        var inviterJob   = inviter?.Character?.Job   ?? 0;
        var inviterLevel = inviter?.Character?.Level ?? 0;

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        // Sub-opcode 0x05 is the raw GuildInvite dispatch value from the V95
        // OnGuildResult switch (case 5). It has no named entry in
        // GuildResultOperations because that enum starts at LoadGuild_Done(0x1C).
        packet.WriteByte(0x05);
        packet.WriteString(message.InviterName);
        packet.WriteInt(inviterJob);
        packet.WriteInt(inviterLevel);
        packet.WriteInt(message.InviterID);
        _ = target.Dispatch(packet.Build());
    }
}

