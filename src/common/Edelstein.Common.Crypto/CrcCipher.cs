namespace Edelstein.Common.Crypto;

/// <summary>
/// Transport-layer CRC cipher for per-packet integrity checking.
/// The client appends a 4-byte CRC tail to every encrypted outgoing packet.
/// The server verifies this tail and advances the session CRC key each packet.
/// TODO(RE #4): Confirm exact polynomial, seed init, final XOR, and key derivation from binary dump.
/// </summary>
public static class CrcCipher
{
    // TODO(RE #4 Q1): Confirm polynomial — standard IEEE 0xEDB88320 assumed until disassembly confirms.
    private static readonly uint[] Table = BuildTable();

    private static readonly IGCipher IGCipher = new();

    /// <summary>
    /// Advances the per-session CRC key after each received packet.
    /// TODO(RE #4 Q4): Confirm advancement formula — may use a dedicated derivation instead of IGCipher.
    /// </summary>
    public static uint AdvanceKey(uint crcKey) => IGCipher.Hash(crcKey, 4, 0);

    /// <summary>
    /// Computes the CRC32 of <paramref name="buffer"/> seeded with <paramref name="crcKey"/>.
    /// TODO(RE #4 Q1-Q3): Confirm seed init (crcKey ^ 0xFFFFFFFF vs crcKey) and final XOR.
    /// </summary>
    public static uint Compute(ReadOnlySpan<byte> buffer, uint crcKey)
    {
        var crc = crcKey ^ 0xFFFFFFFF;
        foreach (var b in buffer)
            crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the trailing 4 bytes of <paramref name="buffer"/>
    /// equal the CRC of the preceding bytes seeded by <paramref name="crcKey"/>.
    /// TODO(RE #4 Q5): Confirm minimum packet length that carries a CRC tail.
    /// </summary>
    public static bool Verify(ReadOnlySpan<byte> buffer, uint crcKey)
    {
        if (buffer.Length < 4)
            return false;
        var data = buffer[..^4];
        var tail = BitConverter.ToUInt32(buffer[^4..]);
        return Compute(data, crcKey) == tail;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var entry = i;
            for (var j = 0; j < 8; j++)
                entry = (entry & 1) != 0 ? (entry >> 1) ^ 0xEDB88320u : entry >> 1;
            table[i] = entry;
        }
        return table;
    }
}
