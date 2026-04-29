using System.Buffers;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Edelstein.Common.Crypto;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Network.Transports;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Network.DotNetty.Codecs;

public class NettyPacketDecoder : ReplayingDecoder<NettyPacketState>
{
    private readonly AESCipher _aesCipher;
    private readonly IGCipher _igCipher;
    private readonly ILogger<NettyPacketDecoder> _logger;
    private readonly TransportVersion _version;
    private short _length;

    private short _sequence;

    public NettyPacketDecoder(
        TransportVersion version,
        AESCipher aesCipher,
        IGCipher igCipher,
        ILogger<NettyPacketDecoder> logger
    )
        : base(NettyPacketState.DecodingHeader)
    {
        _version = version;
        _aesCipher = aesCipher;
        _igCipher = igCipher;
        _logger = logger;
    }

    protected override void Decode(
        IChannelHandlerContext context,
        IByteBuffer input,
        List<object> output
    )
    {
        var socket = context.Channel.GetAttribute(NettyAttributes.SocketKey).Get();

        switch (State)
        {
            case NettyPacketState.DecodingHeader:
                if (socket != null)
                {
                    if (input.ReadableBytes < 4)
                    {
                        RequestReplay();
                        return;
                    }

                    var sequence = input.ReadShortLE();
                    var length = input.ReadShortLE();

                    if (socket.IsDataEncrypted)
                        length ^= sequence;

                    _sequence = sequence;
                    _length = length;
                }
                else
                {
                    if (input.ReadableBytes < 2)
                    {
                        RequestReplay();
                        return;
                    }

                    _length = input.ReadShortLE();
                }

                Checkpoint(NettyPacketState.DecodingPayload);
                return;
            case NettyPacketState.DecodingPayload:
                if (input.ReadableBytes < _length)
                {
                    RequestReplay();
                    return;
                }

                var buffer = ArrayPool<byte>.Shared.Rent(_length);

                input.ReadBytes(buffer, 0, _length);
                Checkpoint(NettyPacketState.DecodingHeader);

                if (_length < 0x2)
                    return;

                var packetLength = _length;

                if (socket != null)
                {
                    var seqRecv = socket.SeqRecv;
                    var version = (short)(seqRecv >> 16) ^ _sequence;

                    if (!(version == -(_version.Major + 1) || version == _version.Major))
                        return;

                    if (socket.IsDataEncrypted)
                    {
                        if (socket.CrcKey != 0)
                        {
                            if (!CrcCipher.Verify(buffer.AsSpan(0, _length), socket.CrcKey))
                            {
                                _logger.LogWarning(
                                    "Transport CRC mismatch from {Remote} — disconnecting",
                                    socket.AddressRemote
                                );
                                ArrayPool<byte>.Shared.Return(buffer);
                                _ = socket.Close();
                                return;
                            }

                            packetLength = (short)(_length - 4);
                            socket.CrcKey = CrcCipher.AdvanceKey(socket.CrcKey);
                        }

                        _aesCipher.Transform(buffer, packetLength, seqRecv);
                        ShandaCipher.DecryptTransform(buffer, packetLength);
                    }

                    socket.SeqRecv = _igCipher.Hash(seqRecv, 4, 0);
                }

                output.Add(new Packet(buffer, packetLength));
                ArrayPool<byte>.Shared.Return(buffer);
                return;
        }
    }
}
