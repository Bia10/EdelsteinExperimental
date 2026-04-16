using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Utilities.Packets;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

/// <summary>
/// Handles inbound <c>CP_GuildResult (0x96)</c> packets from the client.
/// The client uses this opcode exclusively to decline pending guild invitations.
/// <list type="bullet">
///   <item><term>0x37</term><description>Blocked user / decline: InviterName + MyName</description></item>
///   <item><term>0x38</term><description>Already invited / in-guild: InviterName + MyName</description></item>
/// </list>
/// </summary>
public class GuildResultHandler : AbstractFieldHandler
{
    private readonly ILogger _logger;

    public GuildResultHandler(ILogger<GuildResultHandler> logger) => _logger = logger;

    public override short Operation => (short)PacketRecvOperations.GuildResult;

    protected override Task Handle(IFieldUser user, IPacketReader reader)
    {
        var type = (GuildResultOperations)reader.ReadByte();

        switch (type)
        {
            // 0x37 — Player explicitly declined the invitation.
            case GuildResultOperations.InviteGuild_BlockedUser:
            {
                var inviterName = reader.ReadString();
                reader.ReadString(); // myName — redundant; we trust the session
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildRejectResult.Process(
                    new FieldOnPacketGuildRejectResult(user, inviterName, false)
                );
            }

            // 0x38 — Player was already in a guild or already had a pending invite.
            case GuildResultOperations.InviteGuild_AlreadyInvited:
            {
                var inviterName = reader.ReadString();
                reader.ReadString(); // myName — redundant
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildRejectResult.Process(
                    new FieldOnPacketGuildRejectResult(user, inviterName, true)
                );
            }

            default:
                _logger.LogDebug(
                    "Unhandled CP_GuildResult sub-opcode 0x{Type:X2} from character {Name}",
                    (byte)type,
                    user.Character.Name
                );
                return Task.CompletedTask;
        }
    }
}
