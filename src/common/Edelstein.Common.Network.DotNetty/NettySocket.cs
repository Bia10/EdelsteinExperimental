using System.Net;
using DotNetty.Transport.Channels;
using Edelstein.Protocol.Network;
using Edelstein.Protocol.Utilities.Packets;

namespace Edelstein.Common.Network.DotNetty;

public class NettySocket : ISocket
{
    private readonly IChannel _channel;

    public NettySocket(
        IChannel channel,
        uint seqSend,
        uint seqRecv,
        uint crcKey = 0,
        bool isDataEncrypted = true
    )
    {
        _channel = channel;
        SeqSend = seqSend;
        SeqRecv = seqRecv;
        CrcKey = crcKey;
        IsDataEncrypted = isDataEncrypted;
    }

    public string ID => _channel.Id.AsLongText();

    public EndPoint AddressLocal => _channel.LocalAddress;
    public EndPoint AddressRemote => _channel.RemoteAddress;

    public uint SeqSend { get; set; }
    public uint SeqRecv { get; set; }
    public uint CrcKey { get; set; }

    public bool IsDataEncrypted { get; }

    public DateTime LastAliveSent { get; set; }
    public DateTime LastAliveRecv { get; set; }

    public async Task Dispatch(IPacket packet)
    {
        if (_channel.IsWritable)
            await _channel.WriteAndFlushAsync(packet);
    }

    public Task Close() => _channel.DisconnectAsync();
}
