using System.Buffers.Binary;

namespace Edelstein.Common.Crypto;

/// <summary>
/// Transport-layer CRC cipher for per-packet integrity checking.
/// The client appends a 4-byte CRC tail to every encrypted outgoing packet.
/// The server verifies this tail and advances the session CRC key each packet.
/// Algorithm: CRC-32/BZIP2 — polynomial 0x04C11DB7, MSB-first, non-reflected,
/// no init XOR, no final XOR. Source: CCrc32::GetCrc32 (binary 0xC56338).
/// </summary>
public static class CrcCipher
{
    // CRC-32/BZIP2 lookup table — polynomial 0x04C11DB7, MSB-first, non-reflected.
    private static readonly uint[] Table = BuildTable();

    private static readonly IGCipher IGCipher = new();

    /// <summary>
    /// Advances the per-session CRC key after each received packet.
    /// Uses IGCipher.Hash — the same sequence-number derivation path as SeqRecv/SeqSend.
    /// This is architecturally distinct from WZ data CRC seeding (the integer 95 chain).
    /// TODO(RE #4 Q4): Confirm advancement formula from binary — may differ from seq derivation.
    /// </summary>
    public static uint AdvanceKey(uint crcKey) => IGCipher.Hash(crcKey, 4, 0);

    /// <summary>
    /// Computes CRC-32/BZIP2 of <paramref name="buffer"/> using <paramref name="crcKey"/> as
    /// the initial accumulator value (<c>dwInit</c>). No XOR masks are applied on init or output.
    /// </summary>
    public static uint Compute(ReadOnlySpan<byte> buffer, uint crcKey)
    {
        var crc = crcKey;
        foreach (var b in buffer)
            crc = Table[(crc >> 24) ^ b] ^ (crc << 8);
        return crc;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the trailing 4 bytes of <paramref name="buffer"/>
    /// equal the CRC-32/BZIP2 of the preceding bytes seeded by <paramref name="crcKey"/>.
    /// The tail is read as a little-endian uint32 (x86 wire layout).
    /// </summary>
    public static bool Verify(ReadOnlySpan<byte> buffer, uint crcKey)
    {
        if (buffer.Length < 4)
            return false;
        var data = buffer[..^4];
        var tail = BinaryPrimitives.ReadUInt32LittleEndian(buffer[^4..]);
        return Compute(data, crcKey) == tail;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var crc = i << 24;
            for (var j = 0; j < 8; j++)
                crc = (crc & 0x80000000u) != 0 ? (crc << 1) ^ 0x04C11DB7u : crc << 1;
            table[i] = crc;
        }
        return table;
    }
}
