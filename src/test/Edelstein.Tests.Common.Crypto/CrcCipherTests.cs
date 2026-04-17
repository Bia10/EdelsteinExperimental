using Edelstein.Common.Crypto;
using Xunit;

namespace Edelstein.Tests.Common.Crypto;

public class CrcCipherTests
{
    [Fact]
    public void Compute_WithEmptyBuffer_ReturnsKeyXorMask()
    {
        var crcKey = 0u;
        var result = CrcCipher.Compute([], crcKey);
        // Empty buffer: crc starts as crcKey ^ 0xFFFFFFFF, no iterations, then ^ 0xFFFFFFFF → crcKey
        Assert.Equal(crcKey, result);
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
        // Flip a bit in the tail
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
        // Flip a byte in the data portion
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

        var advanced1 = CrcCipher.AdvanceKey(key);
        var advanced2 = CrcCipher.AdvanceKey(key);

        Assert.Equal(advanced1, advanced2);
    }

    [Fact]
    public void AdvanceKey_ProducesDifferentKeyFromInput()
    {
        var key = 0x11223344u;
        var advanced = CrcCipher.AdvanceKey(key);
        Assert.NotEqual(key, advanced);
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
