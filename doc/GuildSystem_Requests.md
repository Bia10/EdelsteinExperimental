# Guild System — Open Requests / Gaps

> **Purpose:** Every item here is something that cannot be determined from the V95 reference
> without guessing. Each entry blocks a specific piece of implementation.  
> Fill in the answers and this file will be consumed by the implementation.

---

## R-001 — GUILDMEMBER struct internal fields (BLOCKER)

**Blocks:** `GuildExtensions.WriteGuildData` (server→client GUILDDATA encoding),
`GuildMemberEntity` field mapping, `GuildMembership` constructor.

**What we know:**
```cpp
struct __unaligned __declspec(align(1)) GUILDMEMBER   // sizeof = 0x25 (37 bytes)
{
    char sCharacterName[13];   // offset 0 — confirmed
    /* Lines 278–283 omitted */
    int  nAllianceGrade;       // last confirmed field
};
```
The 37-byte raw struct is decoded server-side via a single `DecodeBuffer(iPacket, this, 0x25)`.  
We know 13 bytes (name) + 4 bytes (AllianceGrade) = 17 bytes. **20 bytes remain unaccounted.**

**Needed:** The complete field list with types and byte offsets for every field in those
omitted lines (job, level, grade, online/offline, commitment, any padding).

---

## R-002 — Server→Client payload formats for LP_GuildResult (PARTIALLY RESOLVED)

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

**Still unknown:** `RemoveGuild_Done (0x34)` — whether any guildID or other field follows the
sub-opcode before the client clears local guild data.

**Note on CreateGuildAgree sub-opcode:** Section 10 shows client sends `SendCreateGuildAgreeMsg`
with sub-opcode **0x20** (decimal 32), not 0x03. This is a **separate discrepancy** from the
server-side result case 3 (`GuildRes_CreateGuildAgree`). See R-007 for the client-side question.

---

## R-003 — Client request payload for CreateNewGuild (0x04)

Section 10 lists the create-name phase (0x02) and the agree phase (0x20 sub-opcode
listed in table — possible doc inconsistency as enum says `GuildReq_CreateGuildAgree = 0x03`).  
The actual `CreateNewGuild = 0x04` packet payload is not shown.

**Needed:** What does the client send for sub-opcode 0x04?

---

## R-004 — Client request payload for JoinGuild (0x06)

Not listed in Section 10.  
**Needed:** What does the client send for sub-opcode 0x06 (accept pending invite)?

---

## R-005 — Client request payload for RemoveGuild (0x09) / Disband

Not listed in Section 10.  
**Needed:** What does the client send for sub-opcode 0x09?

---

## R-006 — Client request payload for GuildReg_SetSkill (0x1B)

Section 10 shows only `Encode1(27) + ...` — the rest is marked `(guild skill purchase)`.  
**Needed:** What fields follow the sub-opcode byte for skill purchase?  
(SkillID? GP amount? Level to purchase?)

---

## R-007 — CreateGuildAgree sub-opcode inconsistency

Section 10 table shows `SendCreateGuildAgreeMsg` using sub-opcode **0x20** (`Encode1(32)`).  
The V95 enum says `GuildReq_CreateGuildAgree = 0x03`.  
These values do not match.

**Needed:** Which value (0x03 or 0x20) does the client actually write in the first byte  
when a party member agrees/disagrees to forming a guild?

---

## R-008 — ALLIANCEDATA internal fields

Section reference shows `ALLIANCEDATA::Decode` with lines 541–550 omitted.  
Not blocking for phase-1 guild implementation but needed for alliance support.

**Needed:** Full field list of ALLIANCEDATA struct.

---

## R-009 — GUILDDATA additional fields (lines 453–489 omitted in Decode)

The `GUILDDATA::Decode` pseudocode body is largely omitted.  
The wire-layout table in the doc covers all expected fields.  
**Confirm:** Are there any fields in GUILDDATA beyond the wire-layout table?  
(e.g. `sAllianceName`, `aAllianceMemberCount`, or anything not in the table?)

---

*Last updated: 2026-03-03*
