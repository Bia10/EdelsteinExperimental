namespace Edelstein.Common.Crypto;

public static class CrcValidator
{
    private static readonly IGCipher Cipher = new IGCipher();

    /// <summary>Computes IGCipher.Hash(code, 4, 0) and compares to expectedCrc.</summary>
    public static bool Validate(uint code, uint expectedCrc) =>
        Cipher.Hash(code, 4, 0) == expectedCrc;

    /// <summary>Validates the attack packet CRC: Hash(dr2 ^ dr3, 4, 0) == crc.</summary>
    public static bool ValidateAttackDrCrc(int dr2, int dr3, int crc) =>
        Validate((uint)(dr2 ^ dr3), (uint)crc);

    /// <summary>
    /// Validates the two-pass skill-level CRC:
    ///   pass0 = Hash(skillLevel, 4, 0); pass1 = Hash(pass0, 4, 0)
    ///   => crc0 == pass0 AND crc1 == pass1.
    /// NOTE: formula pending disassembly confirmation (issue #2 open question 4).
    /// </summary>
    public static bool ValidateSkillLevelCrc(int skillLevel, int crc0, int crc1)
    {
        var pass0 = Cipher.Hash((uint)skillLevel, 4, 0);
        var pass1 = Cipher.Hash(pass0, 4, 0);
        return (uint)crc0 == pass0 && (uint)crc1 == pass1;
    }

    /// <summary>
    /// Validates the action CRC: Hash(action, 4, 0) == crc.
    /// NOTE: exact field position in packet pending disassembly confirmation (issue #2 open question).
    /// </summary>
    public static bool ValidateActionCrc(int action, int crc) => Validate((uint)action, (uint)crc);

    /// <summary>Validates the per-mob CRC in an attack entry: Hash(mobObjId, 4, 0) == crc.</summary>
    public static bool ValidateMobCrc(int mobObjId, int crc) => Validate((uint)mobObjId, (uint)crc);

    /// <summary>Validates mob-move HackedCode CRC: Hash(hackedCode, 4, 0) == hackedCodeCrc.</summary>
    public static bool ValidateHackedCode(int hackedCode, int hackedCodeCrc) =>
        Validate((uint)hackedCode, (uint)hackedCodeCrc);
}
