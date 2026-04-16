namespace Edelstein.Common.Crypto;

public static class CrcCalculator
{
    private static readonly IGCipher Cipher = new();

    /// <summary>
    /// Computes the MapleStory CRC value by hashing <paramref name="input"/> against <paramref name="key"/>
    /// using the IGCipher hash function (all 4 bytes of input, key as initial state).
    /// </summary>
    public static int Compute(int input, int key) => (int)Cipher.Hash((uint)input, 4, (uint)key);
}
