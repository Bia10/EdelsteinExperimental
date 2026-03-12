using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Utilities.Packets;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

/// <summary>
/// Handles inbound <c>CP_RequestGuildBoardAuthKey (0x121)</c> packets.
/// <para>
/// ⚠ <b>Dead opcode in V95:</b> <c>COutPacket(289)</c> never appears in the V95
/// binary. The server must push the auth key proactively via
/// <c>LP_GuildResult GuildRes_Authkey_Update (0x50)</c> (e.g. on guild load),
/// not in response to a client request. This handler is kept as a fallback only.
/// </para>
/// The client stores the received key in <c>CWvsContext::m_sGuildBoardAuthkey</c>
/// (decoded as narrow string, then widened to <c>ZXString&lt;unsigned short&gt;</c>)
/// and uses it when opening the web-based guild board (<c>CWndGuildBoard</c>).
/// </summary>
public class RequestGuildBoardAuthKeyHandler : AbstractFieldHandler
{
    public override short Operation => (short)PacketRecvOperations.RequestGuildBoardAuthKey;

    protected override Task Handle(IFieldUser user, IPacketReader reader)
    {
        if (user.StageUser.Guild == null)
            return Task.CompletedTask;

        // Generate a per-request, non-guessable auth key.
        var authKey = Guid.NewGuid().ToString("N");

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)GuildResultOperations.Authkey_Update);
        packet.WriteString(authKey);
        return user.Dispatch(packet.Build());
    }
}
