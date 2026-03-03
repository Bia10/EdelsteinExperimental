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

## R-002 — Server→Client payload formats for LP_GuildResult (BLOCKER per operation)

Section 11 of the reference doc (`CWvsContext::OnGuildResult`) is fully omitted.  
For each result code below, we need: what bytes follow the sub-opcode byte?

| Result code | Value | Needed payload format |
|------------|-------|----------------------|
| `CreateGuildAgree_Reply` | 0x20 | ? |
| `CreateNewGuild_Done` | 0x22 | GUILDDATA? ACK only? |
| `JoinGuild_Done` | 0x29 | GUILDDATA? IGuildMember? |
| `WithdrawGuild_Done` | 0x2E | charID + charName? full GUILDDATA? |
| `KickGuild_Done` | 0x31 | charID + charName? |
| `RemoveGuild_Done` | 0x34 | guildID only? |
| `ChangeLevelOrJob` | 0x3E | charID + level + job? |
| `NotifyLoginOrLogout` | 0x3F | charID + channel? |
| `SetGradeName_Done` | 0x40 | 5 × grade name strings? |
| `SetMemberGrade_Done` | 0x42 | charID + grade? |
| `SetMark_Done` | 0x45 | markBg + bgColor + mark + markColor? |
| `SetNotice_Done` | 0x47 | notice string? |
| `SetSkill_Done` | 0x51 | skillID + SKILLENTRY? |
| `InviteGuild_Rejected` | 0x39 | inviterID / name? |

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
