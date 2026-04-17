using Edelstein.Common.Crypto;
using Xunit;

namespace Edelstein.Tests.Common.Crypto;

public class CrcCipherTests
{
    [Fact]
    public void Compute_WithEmptyBuffer_ReturnsKey()
    {
        var crcKey = 0u;
        var result = CrcCipher.Compute([], crcKey);
        // CRC-32/BZIP2: init = crcKey, no iterations, no final XOR -> returns crcKey unchanged.
        Assert.Equal(crcKey, result);
    }

    [Fact]
    public void Compute_Seed95_MatchesReferenceVector()
    {
        // V95_CRC_Complete_Reference.md s2.1: GetCrc32(&{95}, 4, 0) == 0xC36FDB97
        // 95 = 0x5F in little-endian 4 bytes.
        var data = new byte[] { 0x5F, 0x00, 0x00, 0x00 };
        Assert.Equal(0xC36FDB97u, CrcCipher.Compute(data, 0u));
    }

    [Fact]
    public void Compute_Step2_MatchesReferenceVector()
    {
        // V95_CRC_Complete_Reference.md s2.1: GetCrc32(&0xC36FDB97, 4, 0) == 0x4A800456
        // 0xC36FDB97 in little-endian = { 0x97, 0xDB, 0x6F, 0xC3 }.
        var data = new byte[] { 0x97, 0xDB, 0x6F, 0xC3 };
        Assert.Equal(0x4A800456u, CrcCipher.Compute(data, 0u));
    }

    [Fact]
    public void Compute_IsDeterministic()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var crcKey = 0xDEADBEEFu;

        var first = CrcCipher.Compute(data, crcKey);
        var second = CrcCipher.Compute(data, crcKey);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_DifferentKeys_ProduceDifferentResults()
    {
        var data = new byte[] { 0x11, 0x22, 0x33 };

        var crc1 = CrcCipher.Compute(data, 0x00000001u);
        var crc2 = CrcCipher.Compute(data, 0x00000002u);

        Assert.NotEqual(crc1, crc2);
    }

    [Fact]
    public void Verify_WithCorrectTail_ReturnsTrue()
    {
        var data = new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23 };
        var crcKey = 0x12345678u;

        var crc = CrcCipher.Compute(data, crcKey);
        var buffer = data.Concat(BitConverter.GetBytes(crc)).ToArray();

        Assert.True(CrcCipher.Verify(buffer, crcKey));
    }

    [Fact]
    public void Verify_WithCorruptedTail_ReturnsFalse()
    {
        var data = new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23 };
        var crcKey = 0x12345678u;

        var crc = CrcCipher.Compute(data, crcKey);
        var buffer = data.Concat(BitConverter.GetBytes(crc)).ToArray();
        buffer[^1] ^= 0xFF;

        Assert.False(CrcCipher.Verify(buffer, crcKey));
    }

    [Fact]
    public void Verify_WithCorruptedData_ReturnsFalse()
    {
        var data = new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23 };
        var crcKey = 0x12345678u;

        var crc = CrcCipher.Compute(data, crcKey);
        var buffer = data.Concat(BitConverter.GetBytes(crc)).ToArray();
        buffer[0] ^= 0xFF;

        Assert.False(CrcCipher.Verify(buffer, crcKey));
    }

    [Fact]
    public void Verify_WithWrongKey_ReturnsFalse()
    {
        var data = new byte[] { 0xAB, 0xCD, 0xEF };
        var crcKey = 0x12345678u;
        var wrongKey = 0x87654321u;

        var crc = CrcCipher.Compute(data, crcKey);
        var buffer = data.Concat(BitConverter.GetBytes(crc)).ToArray();

        Assert.False(CrcCipher.Verify(buffer, wrongKey));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Verify_WithBufferShorterThan4Bytes_ReturnsFalse(int length)
    {
        var buffer = new byte[length];
        Assert.False(CrcCipher.Verify(buffer, 0xABCDEFu));
    }

    [Fact]
    public void AdvanceKey_IsDeterministic()
    {
        var key = 0xCAFEBABEu;
        Assert.Equal(CrcCipher.AdvanceKey(key), CrcCipher.AdvanceKey(key));
    }

    [Fact]
    public void AdvanceKey_ProducesDifferentKeyFromInput()
    {
        var key = 0x11223344u;
        Assert.NotEqual(key, CrcCipher.AdvanceKey(key));
    }

    [Fact]
    public void AdvanceKey_CalledTwice_ProducesDifferentResults()
    {
        var key = 0xAABBCCDDu;
        var step1 = CrcCipher.AdvanceKey(key);
        var step2 = CrcCipher.AdvanceKey(step1);
        Assert.NotEqual(step1, step2);
    }
}
