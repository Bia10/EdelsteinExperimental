using System.Net;
using Edelstein.Protocol.Utilities.Packets;
using Edelstein.Protocol.Utilities.Repositories;

namespace Edelstein.Protocol.Network;

public interface ISocket : IIdentifiable<string>
{
    EndPoint AddressLocal { get; }
    EndPoint AddressRemote { get; }

    uint SeqSend { get; set; }
    uint SeqRecv { get; set; }

    /// <summary>
    /// Per-session rolling key used to verify the transport-layer CRC tail on incoming packets.
    /// A value of 0 disables CRC validation (used for server-to-server connector sockets).
    /// TODO(RE #4 Q2): Confirm whether the initial value is random (server-chosen) or derived from SeqRecv.
    /// </summary>
    uint CrcKey { get; set; }

    bool IsDataEncrypted { get; }

    DateTime LastAliveSent { get; set; }
    DateTime LastAliveRecv { get; set; }

    Task Dispatch(IPacket packet);
    Task Close();
}
