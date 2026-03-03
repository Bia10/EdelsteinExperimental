# Guild System — Open Requests / Gaps

> **Purpose:** Every item here is something that cannot be determined from the V95 reference
> without guessing. Each entry blocks a specific piece of implementation.  
> Fill in the answers and this file will be consumed by the implementation.

---

## R-001 — GUILDMEMBER struct internal fields ✅ RESOLVED

**Blocks:** `GuildExtensions.WriteGuildData` (server→client GUILDDATA encoding),
`GuildMemberEntity` field mapping, `GuildMembership` constructor.

**Answer — complete struct from `game_types.h:24918`:**

```cpp
struct __unaligned __declspec(align(1)) GUILDMEMBER   // sizeof = 0x25 (37 bytes)
{
    char sCharacterName[13];   // offset  0 — 13 bytes
    int  nJob;                 // offset 13 — 4 bytes
    int  nLevel;               // offset 17 — 4 bytes
    int  nGrade;               // offset 21 — 4 bytes (1=Master, 2=SubMaster, 3-5=Member)
    int  bOnLine;              // offset 25 — 4 bytes (0=offline, 1=online)
    int  nCommitment;          // offset 29 — 4 bytes
    int  nAllianceGrade;       // offset 33 — 4 bytes
};                             // total = 37 bytes ✓
```

No padding. All six int fields are 4 bytes each. The 37-byte `DecodeBuffer` fills them in
contiguous memory order exactly as listed.

---

## R-002 — Server→Client payload formats for LP_GuildResult ✅ FULLY RESOLVED

Section 11 of the reference doc (`CWvsContext::OnGuildResult`) **is present** and provides
a complete switch-case table. The table below reflects what is now confirmed from the doc.
Only two items remain uncertain.

| Result code | Hex | Decimal case | Confirmed payload |
|------------|-----|-------------|-------------------|
| `CreateGuildAgree` | 0x03 | 3 | No explicit byte payload — shows creation-agree dialog only |
| `CreateNewGuild_Done` | 0x22 | 34 | Full GUILDDATA (same path as LoadGuild) |
| `JoinGuild_Done` | 0x29 | 41 | guildID(4) + charID(4); if self → client sends sub-0x00 back, awaits LoadGuild; if other → decode GUILDMEMBER (see R-001) |
| `WithdrawGuild_Done` | 0x2E | 46 | guildID(4) + charID(4) + charName(str) |
| `KickGuild_Done` | 0x31 | 49 | Same as Withdraw: guildID(4) + charID(4) + charName(str) |
| `RemoveGuild_Done` | 0x34 | 52 | **UNKNOWN** — doc only describes behavior (clear data), no explicit bytes listed |
| `ChangeLevelOrJob` | 0x3E | 62 | guildID(4) + charID(4) + nLevel(4) + nJob(4) |
| `NotifyLoginOrLogout` | 0x3F | 63 | guildID(4) + charID(4) + bOnLine(1) |
| `SetGradeName_Done` | 0x40 | 64 | guildID(4) + 5 × gradeName(str) |
| `SetMemberGrade_Done` | 0x42 | 66 | guildID(4) + charID(4) + nGrade(1) |
| `SetMark_Done` | 0x45 | 69 | guildID(4) + markBg(2) + bgColor(1) + mark(2) + markColor(1) |
| `SetNotice_Done` | 0x47 | 71 | guildID(4) + notice(str) |
| `SetSkill_Done` | 0x51 | 81 | guildID(4) + nSkillID(4) + SKILLENTRY |
| `InviteGuild_Rejected` | 0x39 | 57 | targetName(str) only (chat 0xACF to inviter) |

**Resolved:** `RemoveGuild_Done (0x34 = case 52)` — payload is `guildID(4)` only.
The client reads `Decode4(guildID)` as a guard (returns if not own guild), then calls
`GUILDDATA::Clear(&this->m_guild)`. No further fields are decoded.
Server payload: `sub-opcode(1) + guildID(4)` — that is the entire packet.

**Note on CreateGuildAgree sub-opcode:** Confirmed 0x20 (decimal 32) is the correct value sent
by `SendCreateGuildAgreeMsg`. See R-007.

**Note on case 3 server payload:** For non-boss party members, case 3 sends:
`partyID(4) + inviterName(str) + guildName(str)`. For the party boss, no data fields are
read by the client (boss just receives a GuildNPCSay dialog prompt).

---

## R-003 — Client request payload for CreateNewGuild (0x04) ✅ RESOLVED

**Answer: This packet is NEVER sent by the V95 client.**

All 11 places where `COutPacket(149)` (CP_GuildRequest) is constructed were examined.
Sub-opcode 0x04 does not appear in any of them. The full list of sub-opcodes actually
sent by the client is: `0x00, 0x02, 0x05, 0x06, 0x07, 0x08, 0x0D, 0x0E, 0x0F, 0x10, 0x20`.

**Creation flow in V95:**
1. Client sends 0x02 (CheckGuildName) with the proposed guild name
2. Server validates; `GuildRes_CheckGuildName_Available (0x1D = 29)` is NOT handled by the
   client switch — it silently falls to default
3. Server immediately follows with case 3 (`GuildRes_CreateGuildAgree`) to all party members
4. Non-boss members show `CCreateGuildAgreeDlg`; their agree/disagree sends 0x20
5. Party boss gets a GuildNPCSay dialog; their client also sends 0x20 via `SendCreateGuildAgreeMsg`
6. Upon unanimous agreement the server creates the guild and sends case 34

**Server implementation note:** Do NOT wait for a 0x04 packet. Guild creation is triggered
entirely by the server after the name check. `GuildReq_CreateNewGuild (0x04)` is a dead
opcode in this version.

---

## R-004 — Client request payload for JoinGuild (0x06) ✅ RESOLVED

**Answer — from `CUIFadeYesNo::OnButtonClicked` case 8 (guild invite accept):**

```
Encode1(6)                            // sub-opcode
Encode4(this->m_dwInviterID)          // guild invitation source charID
Encode4(myCharacterID)                // self character ID (from CWvsContext::m_dwCharacterId)
```

This fires when the player clicks **Accept** on the guild invite fade popup.
The `m_bGameOpt_Guild` flag (checkbox `ID_CTRL_CHECKBOX_GUILDINVITE`) is checked first;
if blocked, the packet is not sent.

---

## R-005 — Client request payload for RemoveGuild (0x09) / Disband ✅ RESOLVED

**Answer: This packet is NEVER sent by the V95 client.**

Same analysis as R-003. Sub-opcode 0x09 does not appear in any `COutPacket(149)` construction
in the entire binary. `GuildReq_RemoveGuild = 0x09` is a dead opcode in this client. Guild
disband in V95 appears to be NPC-script-driven (the NPC triggers the disband server-side
without a client-side packet). Do NOT expect this packet from clients.

---

## R-006 — Client request payload for GuildReg_SetSkill (0x1B) ✅ RESOLVED

**Answer: This packet is NEVER sent by the V95 client.**

Same analysis as R-003 and R-005. Sub-opcode 0x1B does not appear in any `COutPacket(149)`
construction in the binary. `GuildReg_SetSkill = 0x1B` is a dead opcode in this client.

The reference table entry `Encode1(27) + ... (guild skill purchase)` was **speculative** and
is retracted. Guild skill purchase in V95 is handled entirely via NPC script on the server
side; the client receives the result via `GuildRes_SetSkill_Done (0x51 = case 81)` without
ever having sent a corresponding request packet.

**Server implementation note:** Do NOT wait for a 0x1B packet. Guild skill level-up uses
an NPC script flow.

---

## R-007 — CreateGuildAgree sub-opcode inconsistency ✅ RESOLVED

**Answer: The client sends `0x20` (decimal 32). The enum value 0x03 is irrelevant here.**

From `CField::SendCreateGuildAgreeMsg` at `game_pseudocode.c:262019`:

```c
void __thiscall CField::SendCreateGuildAgreeMsg(CField *this, unsigned __int8 bAgree)
{
    COutPacket::COutPacket(&oPacket, 149);
    COutPacket::Encode1(&oPacket, 0x20u);          // ← always 0x20, not 0x03
    COutPacket::Encode4(&oPacket, myCharacterID);  // CWvsContext::m_dwCharacterId
    COutPacket::Encode1(&oPacket, bAgree);         // 1=agree, 0=disagree
    CClientSocket::SendPacket(...);
}
```

The enum symbol `GuildReq_CreateGuildAgree = 0x03` corresponds to the **server→client**
result opcode that triggers the agree dialog (case 3 in `OnGuildResult`). It is the opcode
the server sends TO the client, not what the client sends back. The client's reply always
uses `0x20 = GuildRes_CreateGuildAgree_Reply`, consistent with the reference table.

There is no inconsistency — the two values serve different directions.

---

## R-008 — ALLIANCEDATA internal fields ✅ RESOLVED

**Answer — full struct from `game_types.h:25000` and `ALLIANCEDATA::Decode` at `game_pseudocode.c:218841`:**

```cpp
struct __cppobj ALLIANCEDATA
{
    int nAllianceID;                      // 4 bytes
    ZXString<char> sAllianceName;         // var
    ZArray<ZXString<char>> asGradeName;   // exactly 5 entries (hardcoded loop)
    ZArray<unsigned long> adwGuildID;     // N entries (count from wire Decode1)
    int nMaxMemberNum;                    // 4 bytes
    ZXString<char> sNotice;               // var
};
```

**Wire layout from `ALLIANCEDATA::Decode`:**

| Field | Size | Type |
|-------|------|------|
| nAllianceID | 4 | Decode4 |
| sAllianceName | var | DecodeStr |
| asGradeName[0..4] | var × 5 | DecodeStr (loop runs exactly 5 times) |
| nGuildCount | 1 | Decode1 |
| adwGuildID (4 × nGuildCount) | 4×N | DecodeBuffer (contiguous bulk read) |
| nMaxMemberNum | 4 | Decode4 |
| sNotice | var | DecodeStr |

---

## R-009 — GUILDDATA additional fields ✅ RESOLVED

**Answer: The wire-layout table is complete and accurate. No hidden extra fields.**

Full `GUILDDATA::Decode` reviewed at `game_pseudocode.c:221057`. The decode matches the
reference table exactly:

```
nGuildID(4) → sGuildName(str) → asGradeName[0..4](5×str) → nMemberCount(1)
→ adwCharacterID(4×N bulk) → aMemberData(37×N bulk)
→ nMaxMemberNum(4) → nMarkBg(2) → nMarkBgColor(1) → nMark(2) → nMarkColor(1)
→ sNotice(str) → nPoint(4) → nAllianceID(4) → nLevel(1)
→ nSkillCount(2) → per-skill: nSkillID(4) + SKILLENTRY
```

**`sAllianceName` and `aAllianceMemberCount` do NOT exist in GUILDDATA.** Those fields
belong to the separate `ALLIANCEDATA` struct (see R-008). The alliance member guild list
is stored in `CWvsContext::m_AllianceMember` (a `ZArray<GUILDDATA>`) and populated by
`GUILDDATA::Decode` calls inside `CWvsContext::OnAllianceResult` case `0x0D`, not inside
`GUILDDATA::Decode` itself.

---

*Last updated: 2026-03-03 — All open requests resolved.*
