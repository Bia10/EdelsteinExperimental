// V95Crc.cs — Application-layer CRC systems ported from V95 client decompilation
//
// Algorithm: CRC-32/BZIP2 (polynomial 0x04C11DB7, MSB-first, non-reflected,
//            no final XOR). NOT the common CRC-32 (0xEDB88320 / PKZIP).
//
// Reference vectors (verify implementation correctness):
//   GetCrc32(int32=95,  init=0)        = 0xC36FDB97
//   GetCrc32(0xC36FDB97, init=0)       = 0x4A800456
//   CalcSkillLevelCrc(all zeros)       = 0xBEBA307A
//   CalcFieldCrc(0, 0, 0, 0, 0, 0, 0) = 0x00000000
//
// Source: V95_CRC_Complete_Reference.md (PDB-derived decompilation analysis)

using System;
using System.Collections.Generic;
using System.Text;

namespace Edelstein.Common.Gameplay.Game.Security;

public static class V95Crc
{
    // ─────────────────────────────────────────────────────────────────────────
    // Core CRC-32/BZIP2 primitive (CCrc32::GetCrc32)
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var t = new uint[256];
        for (var i = 0; i < 256; i++)
        {
            var crc = (uint)i << 24;
            for (var j = 0; j < 8; j++)
                crc = (crc & 0x80000000u) != 0 ? (crc << 1) ^ 0x04C11DB7u : crc << 1;
            t[i] = crc;
        }
        return t;
    }

    /// <summary>
    /// Core CRC-32/BZIP2 primitive — exact port of CCrc32::GetCrc32.
    /// Pass <paramref name="init"/> = 0 for the first call in a chain, or the
    /// previous result when chaining fields sequentially.
    /// </summary>
    public static uint GetCrc32(ReadOnlySpan<byte> data, uint init = 0)
    {
        var acc = init;
        foreach (var b in data)
            acc = Table[(acc >> 24) ^ b] ^ (acc << 8);
        return acc;
    }

    // Convenience: CRC32 of a single little-endian int32 (the most common case).
    private static uint Crc32I32(int val, uint init)
    {
        Span<byte> buf = stackalloc byte[4];
        buf[0] = (byte)val;
        buf[1] = (byte)(val >> 8);
        buf[2] = (byte)(val >> 16);
        buf[3] = (byte)(val >> 24);
        return GetCrc32(buf, init);
    }

    // Physics constant double: (int64)(double), low 4 bytes only.
    // Matches the truncating cast `(__int64)d` in client pseudocode.
    private static uint Crc32DoubleTrunc(double val, uint init)
    {
        var truncated = (long)val;
        Span<byte> buf = stackalloc byte[4];
        buf[0] = (byte)truncated;
        buf[1] = (byte)(truncated >> 8);
        buf[2] = (byte)(truncated >> 16);
        buf[3] = (byte)(truncated >> 24);
        return GetCrc32(buf, init);
    }

    // String CRC: raw CP-1252 bytes, no null terminator, chained into init.
    private static readonly Encoding Cp1252 = Encoding.GetEncoding(1252);

    private static uint Crc32String(string s, uint init)
    {
        if (string.IsNullOrEmpty(s))
            return init;
        return GetCrc32(Cp1252.GetBytes(s), init);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §1  Skill Level CRC  (SKILLLEVELDATA::CalcCrc)
    //     Sent: LP_CheckCrcResult (0x17) periodic challenge response
    //     Combination: chaining, seeded with CRC(95)
    // ─────────────────────────────────────────────────────────────────────────

    public static uint CalcSkillLevelCrc(
        int nAction,
        int nSpeed,
        int nJump,
        int nHPCon,
        int nMPCon,
        int nDamage,
        int nFixDamage,
        int nSelfDestruction,
        int nProp,
        int nRange,
        int nMobCount,
        int nAttackCount,
        int nBulletCount,
        int nACC,
        int nCooltime,
        int rcLeft,
        int rcRight,
        int rcTop,
        int rcBottom
    )
    {
        var crc = Crc32I32(95, 0);
        crc = Crc32I32(nAction, crc);
        crc = Crc32I32(nSpeed, crc);
        crc = Crc32I32(nJump, crc);
        crc = Crc32I32(nHPCon, crc);
        crc = Crc32I32(nMPCon, crc);
        crc = Crc32I32(nDamage, crc);
        crc = Crc32I32(nFixDamage, crc);
        crc = Crc32I32(nSelfDestruction, crc);
        crc = Crc32I32(nProp, crc);
        crc = Crc32I32(nRange, crc);
        crc = Crc32I32(nMobCount, crc);
        crc = Crc32I32(nAttackCount, crc);
        crc = Crc32I32(nBulletCount, crc);
        crc = Crc32I32(nACC, crc);
        crc = Crc32I32(nCooltime, crc);
        crc = Crc32I32(rcLeft, crc);
        crc = Crc32I32(rcRight, crc);
        crc = Crc32I32(rcTop, crc);
        crc = Crc32I32(rcBottom, crc);
        return crc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §2  Skill Entry CRC  (SKILLENTRY::InitCrc / AddCrc)
    //     Covers summoned-creature attack spatial data only.
    //     Not sent in any known packet; internal consistency check at load time.
    // ─────────────────────────────────────────────────────────────────────────

    public static uint SkillEntryInitCrc()
    {
        var step1 = Crc32I32(95, 0);
        return Crc32I32((int)step1, 0);
    }

    public static uint SkillEntryAddCrc(uint current, int val) => current ^ Crc32I32(val, 0);

    // ─────────────────────────────────────────────────────────────────────────
    // §3  Item Icon CRC  (CItemInfo::GetItemIconCRC)
    //     Returns 0 for all non-animated (single-frame) items — the correct
    //     client value, not a server approximation.
    //     Non-zero only when BOTH frame 0 and frame 1 carry valid pixel data.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes GetItemIconCRC from raw BGR565 pixel data.
    /// For the vast majority of items, pass null for either frame to get the
    /// correct client result of 0 (the null-frame early-return in the client).
    /// </summary>
    public static uint CalcItemIconCrc(
        IEnumerable<IEnumerable<(byte[] Pixels, int Width)>>? frame0Pixels,
        IEnumerable<IEnumerable<(byte[] Pixels, int Width)>>? frame1Pixels
    )
    {
        if (frame0Pixels == null)
            return 0;
        if (frame1Pixels == null)
            return 0;

        uint dwRet = 0;
        foreach (var framePixels in new[] { frame0Pixels, frame1Pixels })
        foreach (var tile in framePixels)
        foreach (var (rowPixels, pixelWidth) in tile)
        {
            var byteLen = Math.Min(2 * pixelWidth, rowPixels.Length);
            dwRet ^= GetCrc32(rowPixels.AsSpan(0, byteLen), 0);
        }
        return dwRet;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §4  Item Common CRC  (CItemInfo::GetItemCommonCRC)
    //     Base for all item CRCs; XOR-combination, NOT chaining.
    // ─────────────────────────────────────────────────────────────────────────

    public static uint GetItemCommonCrc(int nItemID, uint iconCrc, string itemName, string itemDesc)
    {
        var step1 = Crc32I32(95, 0);
        var key = Crc32I32((int)step1, 0);

        var crc = Crc32I32(nItemID, 0) ^ key;
        crc ^= iconCrc;

        var combined = (itemName ?? string.Empty) + (itemDesc ?? string.Empty);
        if (combined.Length > 0)
            crc ^= Crc32String(combined, 0);

        return crc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §5  Bundle Item CRC  (CItemInfo::GetBundleItemCRC)
    //     Sent: drop pickup, NPC shop buy, trade initiation.
    // ─────────────────────────────────────────────────────────────────────────

    public static uint GetBundleItemCrc(
        int nItemID,
        uint iconCrc,
        string itemName,
        string itemDesc,
        int nRequiredLEV,
        int nSellPrice,
        double dSellUnitPrice,
        int nMaxPerSlot,
        int nMax,
        bool bQuest,
        bool bPartyQuest,
        bool bTimeLimited,
        bool bTradeBlock,
        int nAppliableKarmaType,
        bool bNotSale,
        bool bExpireOnLogout,
        bool bNoCancelMouse
    )
    {
        var crc = GetItemCommonCrc(nItemID, iconCrc, itemName, itemDesc);

        // Group 1: nRequiredLEV | nSellPrice | dSellUnitPrice(8 bytes)
        Span<byte> dBuf = stackalloc byte[8];
        var rawU = BitConverter.DoubleToUInt64Bits(dSellUnitPrice);
        for (var i = 0; i < 8; i++)
            dBuf[i] = (byte)(rawU >> (i * 8));

        crc ^= Crc32I32(nRequiredLEV, 0) | Crc32I32(nSellPrice, 0) | GetCrc32(dBuf, 0);

        // Group 2: nMaxPerSlot | nMax
        crc ^= Crc32I32(nMaxPerSlot, 0) | Crc32I32(nMax, 0);

        // Packed flag byte — nested 2* multiply, outermost = bit 0 = bNoCancelMouse
        var flagByte =
            (bNoCancelMouse ? 1 : 0)
            + 2
                * (
                    (bExpireOnLogout ? 1 : 0)
                    + 2
                        * (
                            (bNotSale ? 1 : 0)
                            + 2
                                * (
                                    (nAppliableKarmaType != 0 ? 1 : 0)
                                    + 2
                                        * (
                                            (bTradeBlock ? 1 : 0)
                                            + 2
                                                * (
                                                    (bTimeLimited ? 1 : 0)
                                                    + 2
                                                        * (
                                                            (bPartyQuest ? 1 : 0)
                                                            + 2 * (bQuest ? 1 : 0)
                                                        )
                                                )
                                        )
                                )
                        )
                );
        crc ^= Crc32I32(flagByte, 0);
        return crc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §6  Equip Item CRC  (CItemInfo::RegisterEquipItemInfo)
    //     Sent: equip-related transactions.
    // ─────────────────────────────────────────────────────────────────────────

    public static uint CalcEquipItemCrc(
        int nItemID,
        uint iconCrc,
        string itemName,
        string itemDesc,
        int nrSTR,
        int nrDEX,
        int nrINT,
        int nrLUK,
        int nrPOP,
        int nrJob,
        int nrLevel,
        int nrMobLevel,
        int nSellPrice,
        double dRecovery,
        double dFs,
        int nKnockback,
        int nSwim,
        bool bEpic,
        bool bNotExtend,
        bool bExpireOnLogout,
        bool bNotSale,
        bool bAccountSharable,
        int nAppliableKarmaType,
        bool bTradeBlock,
        bool bOnlyEquip,
        bool bOnly,
        bool bTimeLimited
    )
    {
        var crc = GetItemCommonCrc(nItemID, iconCrc, itemName, itemDesc);

        // Group A: nrSTR | nrDEX | nrINT
        crc ^= Crc32I32(nrSTR, 0) | Crc32I32(nrDEX, 0) | Crc32I32(nrINT, 0);

        // Group B: nrLUK | nrPOP | nrJob
        crc ^= Crc32I32(nrLUK, 0) | Crc32I32(nrPOP, 0) | Crc32I32(nrJob, 0);

        // Group C: nrLevel | nrMobLevel | nSellPrice
        crc ^= Crc32I32(nrLevel, 0) | Crc32I32(nrMobLevel, 0) | Crc32I32(nSellPrice, 0);

        // Group D: dRecovery(8 bytes) | dFs(8 bytes) — full IEEE-754, NOT truncated
        Span<byte> buf8 = stackalloc byte[8];
        var rawR = BitConverter.DoubleToUInt64Bits(dRecovery);
        for (var i = 0; i < 8; i++)
            buf8[i] = (byte)(rawR >> (i * 8));
        var recCrc = GetCrc32(buf8, 0);

        var rawF = BitConverter.DoubleToUInt64Bits(dFs);
        for (var i = 0; i < 8; i++)
            buf8[i] = (byte)(rawF >> (i * 8));
        var fsCrc = GetCrc32(buf8, 0);

        crc ^= recCrc | fsCrc;

        // Group E: nKnockback | nSwim
        crc ^= Crc32I32(nKnockback, 0) | Crc32I32(nSwim, 0);

        // Packed flag byte (10 flags, outermost = bit 0 = bEpic)
        var flagByte =
            (bEpic ? 1 : 0)
            + 2
                * (
                    (bNotExtend ? 1 : 0)
                    + 2
                        * (
                            (bExpireOnLogout ? 1 : 0)
                            + 2
                                * (
                                    (bNotSale ? 1 : 0)
                                    + 2
                                        * (
                                            (bAccountSharable ? 1 : 0)
                                            + 2
                                                * (
                                                    (nAppliableKarmaType != 0 ? 1 : 0)
                                                    + 2
                                                        * (
                                                            (bTradeBlock ? 1 : 0)
                                                            + 2
                                                                * (
                                                                    (bOnlyEquip ? 1 : 0)
                                                                    + 2
                                                                        * (
                                                                            (bOnly ? 1 : 0)
                                                                            + 2
                                                                                * (
                                                                                    bTimeLimited
                                                                                        ? 1
                                                                                        : 0
                                                                                )
                                                                        )
                                                                )
                                                        )
                                                )
                                        )
                                )
                        )
                );
        crc ^= Crc32I32(flagByte, 0);
        return crc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §7  Foothold Geometry CRC  (CWvsPhysicalSpace2D::m_dwCRC)
    //     Combination: chaining (init = 0, no seed-95 here)
    // ─────────────────────────────────────────────────────────────────────────

    public static uint CalcFootholdCrc(ReadOnlySpan<FootholdEntry> footholds)
    {
        uint acc = 0;
        foreach (ref readonly var fh in footholds)
        {
            acc = Crc32I32(fh.X1, acc);
            acc = Crc32I32(fh.Y1, acc);
            acc = Crc32I32(fh.X2, acc);
            acc = Crc32I32(fh.Y2, acc);
            acc = Crc32I32(fh.Drag, acc);
            acc = Crc32I32(fh.Force, acc);
            acc = Crc32I32(fh.ForbidFallDown, acc);
            acc = Crc32I32(fh.CantThrough, acc);
            acc = Crc32I32(fh.SNPrev, acc);
            acc = Crc32I32(fh.SNNext, acc);
            acc = Crc32I32(fh.SN, acc);
        }
        return acc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §8  Portal List CRC  (CPortalList::m_dwPortalCrc)
    //     All portals contribute — including type 6 (TownPortalPoint).
    //     The nType != 6 check in the client controls only the navigation array
    //     insertion, NOT the CRC chain. (V95_CRC_Complete_Reference.md §10.3)
    // ─────────────────────────────────────────────────────────────────────────

    public static uint CalcPortalCrc(int fieldId, ReadOnlySpan<PortalEntry> portals)
    {
        var crc = Crc32I32(fieldId, 0);

        Span<byte> ptBuf = stackalloc byte[8];
        Span<byte> oneBuf = stackalloc byte[1];

        foreach (ref readonly var p in portals)
        {
            crc = Crc32String(p.SName, crc);
            crc = Crc32I32(p.NType, crc);

            ptBuf[0] = (byte)p.X;
            ptBuf[1] = (byte)(p.X >> 8);
            ptBuf[2] = (byte)(p.X >> 16);
            ptBuf[3] = (byte)(p.X >> 24);
            ptBuf[4] = (byte)p.Y;
            ptBuf[5] = (byte)(p.Y >> 8);
            ptBuf[6] = (byte)(p.Y >> 16);
            ptBuf[7] = (byte)(p.Y >> 24);
            crc = GetCrc32(ptBuf, crc);

            crc = Crc32I32(p.NHRange, crc);
            crc = Crc32I32(p.NVRange, crc);
            crc = Crc32I32(p.NTMap, crc);
            crc = Crc32String(p.STName, crc);
            crc = Crc32I32(p.NDelayTime, crc);

            oneBuf[0] = p.BOnlyOnce ? (byte)1 : (byte)0;
            crc = GetCrc32(oneBuf, crc);

            crc = Crc32I32(p.NVImpact, crc);
            crc = Crc32I32(p.NHImpact, crc);
        }
        return crc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §9  Field Composite CRC  (CField::m_dwCrc)
    //     Embedded in every movement packet (CMovePath::Flush).
    // ─────────────────────────────────────────────────────────────────────────

    public static uint CalcFieldCrc(
        uint footholdCrc,
        uint portalCrc,
        int bTown,
        int bSwim,
        int bFly,
        int bPersonalShopAvailable,
        int nPhase
    )
    {
        var acc = footholdCrc ^ portalCrc;
        acc = Crc32I32(bTown, acc);
        acc = Crc32I32(bSwim, acc);
        acc = Crc32I32(bFly, acc);
        acc = Crc32I32(bPersonalShopAvailable, acc);
        acc = Crc32I32(nPhase, acc);
        return acc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §10  Physics Constants CRC  (CWvsPhysicalSpace2D::GetConstantCRC)
    //      Separate from field CRC; seeded with CRC(95).
    //      Each double truncated via (int64)d before taking low 4 bytes.
    // ─────────────────────────────────────────────────────────────────────────

    public static uint CalcPhysicsConstantCrc(
        double dWalkForce,
        double dWalkSpeed,
        double dWalkDrag,
        double dSlipForce,
        double dSlipSpeed,
        double dFloatDrag1,
        double dFloatCoeff,
        double dSwimForce,
        double dSwimSpeed,
        double dFlyForce,
        double dFlySpeed,
        double dGravityAcc,
        double dFallSpeed,
        double dJumpSpeed,
        double dMaxFriction,
        double dMinFriction,
        double dSwimSpeedDec,
        double dFlyJumpDec
    )
    {
        var crc = Crc32I32(95, 0);
        crc = Crc32DoubleTrunc(dWalkForce, crc);
        crc = Crc32DoubleTrunc(dWalkSpeed, crc);
        crc = Crc32DoubleTrunc(dWalkDrag, crc);
        crc = Crc32DoubleTrunc(dSlipForce, crc);
        crc = Crc32DoubleTrunc(dSlipSpeed, crc);
        crc = Crc32DoubleTrunc(dFloatDrag1, crc);
        crc = Crc32DoubleTrunc(dFloatCoeff, crc);
        crc = Crc32DoubleTrunc(dSwimForce, crc);
        crc = Crc32DoubleTrunc(dSwimSpeed, crc);
        crc = Crc32DoubleTrunc(dFlyForce, crc);
        crc = Crc32DoubleTrunc(dFlySpeed, crc);
        crc = Crc32DoubleTrunc(dGravityAcc, crc);
        crc = Crc32DoubleTrunc(dFallSpeed, crc);
        crc = Crc32DoubleTrunc(dJumpSpeed, crc);
        crc = Crc32DoubleTrunc(dMaxFriction, crc);
        crc = Crc32DoubleTrunc(dMinFriction, crc);
        crc = Crc32DoubleTrunc(dSwimSpeedDec, crc);
        crc = Crc32DoubleTrunc(dFlyJumpDec, crc);
        return crc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §11  Mob Template CRC  (CMobTemplate::CalcCrc)
    //      Priority 3 — skip on private servers (requires server-managed key
    //      rotation via LP_MobCrcKeyChanged / CP_MobCrcKeyChangedReply).
    //      When dwKey = 0 (no rotation), this produces the correct value for
    //      clients that have never received a key-change packet.
    // ─────────────────────────────────────────────────────────────────────────

    public static uint CalcMobTemplateCrc(
        uint dwKey,
        bool bBodyAttack,
        int nLevel,
        int nMaxHP,
        int nMaxMP,
        int nSpeed,
        int nFlySpeed,
        bool bOnlyNormalAttack,
        int nChaseSpeed,
        int nPAD,
        int nPDR,
        bool bNotAttack,
        int nMAD,
        int nMDR,
        int nACC,
        bool bSelfDestruction,
        int nEVA,
        int nEXP,
        int nPushedDamage,
        bool bPickUpDrop,
        int nEscortType,
        ReadOnlySpan<int> aDamagedElemAttr,
        int nHPRecovery,
        int nMPRecovery,
        bool bFirstAttack,
        double nFs,
        bool bInvincible,
        int nFixedDamage,
        uint dwTemplateID,
        ReadOnlySpan<MobAttackEntry> attacks,
        ReadOnlySpan<MobSkillEntry> skills
    )
    {
        var acc = Crc32I32((int)dwKey, 0);

        acc ^= Crc32I32(bBodyAttack ? 1 : 0, 0) | Crc32I32(nLevel, 0) | Crc32I32(nMaxHP, 0);
        acc ^=
            Crc32I32(nMaxMP, 0)
            | Crc32I32(nSpeed, 0)
            | Crc32I32(nFlySpeed, 0)
            | Crc32I32(bOnlyNormalAttack ? 1 : 0, 0);
        acc ^=
            Crc32I32(nChaseSpeed, 0)
            | Crc32I32(nPAD, 0)
            | Crc32I32(nPDR, 0)
            | Crc32I32(bNotAttack ? 1 : 0, 0);
        acc ^=
            Crc32I32(nMAD, 0)
            | Crc32I32(nMDR, 0)
            | Crc32I32(nACC, 0)
            | Crc32I32(bSelfDestruction ? 1 : 0, 0);
        acc ^=
            Crc32I32(nEVA, 0)
            | Crc32I32(nEXP, 0)
            | Crc32I32(nPushedDamage, 0)
            | Crc32I32(bPickUpDrop ? 1 : 0, 0)
            | Crc32I32(nEscortType, 0);

        uint elemGroup = 0;
        for (var i = 0; i < Math.Min(aDamagedElemAttr.Length, 8); i++)
            elemGroup |= Crc32I32(aDamagedElemAttr[i], 0);
        acc ^= elemGroup;

        acc ^=
            Crc32I32(nHPRecovery, 0) | Crc32I32(nMPRecovery, 0) | Crc32I32(bFirstAttack ? 1 : 0, 0);

        Span<byte> buf8 = stackalloc byte[8];
        var rawFs = BitConverter.DoubleToUInt64Bits(nFs);
        for (var i = 0; i < 8; i++)
            buf8[i] = (byte)(rawFs >> (i * 8));
        acc ^= GetCrc32(buf8, 0) | Crc32I32(bInvincible ? 1 : 0, 0) | Crc32I32(nFixedDamage, 0);

        acc ^= Crc32I32((int)dwTemplateID, 0);

        foreach (ref readonly var atk in attacks)
        {
            var aGroup =
                Crc32I32(atk.NType, 0)
                | Crc32I32(atk.BInactive ? 1 : 0, 0)
                | Crc32I32(atk.NConMP, 0)
                | Crc32I32(atk.BMagicAttack ? 1 : 0, 0);
            var bGroup =
                Crc32I32(atk.BJumpAttack ? 1 : 0, 0)
                | Crc32I32(atk.BMagicAttack ? 1 : 0, 0)
                | Crc32I32(atk.NBulletSpeed, 0)
                | Crc32I32(atk.NBulletNumber, 0);
            var cGroup =
                Crc32I32(atk.BDeadlyAttack ? 1 : 0, 0)
                | Crc32I32(atk.BTremble ? 1 : 0, 0)
                | Crc32I32(atk.BDoFirst ? 1 : 0, 0)
                | Crc32I32(atk.NMPBurn, 0);
            var dGroup =
                Crc32I32(atk.BKnockBack ? 1 : 0, 0)
                | Crc32I32(atk.TRandDelayAttack, 0)
                | Crc32I32(atk.BRush ? 1 : 0, 0);
            acc ^= aGroup ^ bGroup ^ cGroup ^ dGroup ^ Crc32I32(atk.TAttackAfter, 0);

            if (atk.NType == 0)
                break;
        }

        foreach (ref readonly var sk in skills)
            acc ^=
                Crc32I32(sk.NSkillID, 0)
                | Crc32I32(sk.NSLV, 0)
                | Crc32I32(sk.NAction, 0)
                | Crc32I32(sk.TEffectAfter, 0);

        if (acc == 0xFFFFFFFF)
            acc = 0;

        return acc;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Data-transfer structs — populate from WZ loaders and pass to V95Crc methods
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Fields from a Map.wz foothold that contribute to the geometry CRC.</summary>
public readonly struct FootholdEntry
{
    public readonly int SN;
    public readonly int X1,
        Y1,
        X2,
        Y2;

    /// <summary>Raw WZ integer (before /100 conversion to double).</summary>
    public readonly int Drag;

    /// <summary>Raw WZ integer (before /100 conversion to double).</summary>
    public readonly int Force;
    public readonly int ForbidFallDown;
    public readonly int CantThrough;
    public readonly int SNPrev;
    public readonly int SNNext;

    public FootholdEntry(
        int sn,
        int x1,
        int y1,
        int x2,
        int y2,
        int drag,
        int force,
        int forbidFallDown,
        int cantThrough,
        int snPrev,
        int snNext
    )
    {
        SN = sn;
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        Drag = drag;
        Force = force;
        ForbidFallDown = forbidFallDown;
        CantThrough = cantThrough;
        SNPrev = snPrev;
        SNNext = snNext;
    }
}

/// <summary>Fields from a Map.wz portal that contribute to the portal CRC.</summary>
public readonly struct PortalEntry
{
    public readonly string SName;
    public readonly int NType;
    public readonly int X,
        Y;
    public readonly int NHRange;
    public readonly int NVRange;
    public readonly int NTMap;
    public readonly string STName;
    public readonly int NDelayTime;
    public readonly bool BOnlyOnce;
    public readonly int NVImpact;
    public readonly int NHImpact;

    public PortalEntry(
        string sName,
        int nType,
        int x,
        int y,
        int nHRange,
        int nVRange,
        int nTMap,
        string sTName,
        int nDelayTime,
        bool bOnlyOnce,
        int nVImpact,
        int nHImpact
    )
    {
        SName = sName;
        NType = nType;
        X = x;
        Y = y;
        NHRange = nHRange;
        NVRange = nVRange;
        NTMap = nTMap;
        STName = sTName ?? string.Empty;
        NDelayTime = nDelayTime;
        BOnlyOnce = bOnlyOnce;
        NVImpact = nVImpact;
        NHImpact = nHImpact;
    }
}

/// <summary>Per-attack action entry for mob template CRC (§11).</summary>
public readonly struct MobAttackEntry
{
    public int NType { get; init; }
    public bool BInactive { get; init; }
    public int NConMP { get; init; }
    public bool BMagicAttack { get; init; }
    public bool BJumpAttack { get; init; }
    public int NBulletSpeed { get; init; }
    public int NBulletNumber { get; init; }
    public bool BDeadlyAttack { get; init; }
    public bool BTremble { get; init; }
    public bool BDoFirst { get; init; }
    public int NMPBurn { get; init; }
    public bool BKnockBack { get; init; }
    public int TRandDelayAttack { get; init; }
    public bool BRush { get; init; }
    public int TAttackAfter { get; init; }
}

/// <summary>Per-skill cast entry for mob template CRC (§11).</summary>
public readonly struct MobSkillEntry
{
    public int NSkillID { get; init; }
    public int NSLV { get; init; }
    public int NAction { get; init; }
    public int TEffectAfter { get; init; }
}
