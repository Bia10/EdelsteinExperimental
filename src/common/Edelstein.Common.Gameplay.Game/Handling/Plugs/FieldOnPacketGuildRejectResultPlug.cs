using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles a player declining a pending guild invitation
/// (CP_GuildResult 0x37 or 0x38 from the client).
/// Calls <c>InviteReject</c> on the service, which removes the DB invitation
/// and publishes <c>NotifyGuildInviteRejected</c> so the original inviter
/// receives <c>InviteGuild_Rejected (0x39)</c>.
/// </summary>
public class FieldOnPacketGuildRejectResultPlug : IPipelinePlug<FieldOnPacketGuildRejectResult>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildRejectResult message)
    {
        // GuildID is not used by the service's reject lookup (it queries by CharacterID),
        // so passing 0 is safe and avoids needing an extra DB round-trip.
        await message.User.StageUser.Context.Services.Guild.InviteReject(
            new GuildInviteRejectRequest(
                0,
                message.User.Character.ID,
                message.User.Character.Name
            ));
    }
}
