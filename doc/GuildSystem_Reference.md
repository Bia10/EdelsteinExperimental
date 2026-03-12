# Guild & Alliance System — V95 Client Reference

> **Source**: `V95ClientRe/game_pseudocode.c` (IDA/Hex-Rays decompilation, 1,337,965 lines), `V95ClientRe/game_types.h` (PDB structs/enums)
> **Packet headers**: Guild request = `CP_GuildRequest` (149), Guild response = `CP_GuildResult` (150); Alliance request = 167/168
> **Dispatch opcodes**: `CWvsContext::ProcessPacket` — case 67 → `OnGuildResult`, case 68 → `OnAllianceResult`, case 59 → `OnGuildBBSPacket`

---

## RE Research Requests

> All items resolved. See Section 13 for full BBS packet layouts.

### BSS-1 [DONE] — `CP_GuildBBS (0xB3)`: client→server sub-opcode enumeration

**Answer:** Six sub-opcodes (0–5). See Section 13 for the complete table.

---

### BSS-2 [DONE] — `LP_GuildBBS (0x3B)` case 6: `OnLoadListResult` packet layout

**Answer:** Confirmed from `CUIGuildBBS::OnLoadListResult` at `game_pseudocode.c:803710`. See Section 13.

---

### BSS-3 [DONE] — `LP_GuildBBS (0x3B)` case 7: `OnViewEntryResult` packet layout

**Answer:** Confirmed from `CUIGuildBBS::OnViewEntryResult` at `game_pseudocode.c:805097`. See Section 13.

---

### BSS-4 [DONE] — `LP_GuildBBS (0x3B)` case 8: `OnEntryNotFound` packet layout

**Answer:** No payload fields at all. Server sends only the sub-opcode byte (8). `CUIGuildBBS::OnEntryNotFound(void)` takes no `CInPacket` parameter — it simply shows StringPool 0xEC7 and clears the current entry. See Section 13.

---

### BSS-5 [DONE] — `CP_RequestGuildBoardAuthKey (0x121)` payload & auth key response format

**Answer: `CP_RequestGuildBoardAuthKey (0x121 = 289)` is a dead opcode** — never sent by the V95 client. `COutPacket(289)` does not appear anywhere in `game_pseudocode.c`. The server pushes the auth key proactively via case 80 (`GuildRes_Authkey_Update`), decoded as a **narrow char string** then converted to UTF-16 for storage in `m_sGuildBoardAuthkey`. The timestamp `m_dwGuildBoardAuthkeyLastUpdated` records receipt time; no client-side request is involved.

---

### BSS-6 [DONE] — `CUIGuildBBS` struct and UI state machine

**Answer:** `CUIGuildBBS` (`game_types.h:32396`) and `CWndGuildBoard` (`game_types.h:47691`) are **completely separate classes** with no shared state. `CUIGuildBBS` is a fully in-game BBS (no web view). Guild tab button ID `0x7FA` calls `CWvsContext::UI_Toggle(0x27, -1)` to open `CUIGuildBBS`. `CWndGuildBoard` is a `CWebWnd` (IE embedded). There is no `nMaskPageType` / `nCodeBoard` in `CUIGuildBBS` — those fields belong exclusively to `CWndGuildBoard`. See Section 13.

---

### BSS-7 [DONE] — Guild Skill activation: `GuildReg_SetSkill (0x1B)` client payload

**Answer:** Dead opcode — see Section 10 and resolved in `GuildSystem_Requests.md R-006`.

---

### BSS-8 [DONE] — `GuildRes_GuildSkillResult (case 79)` full decode

**Answer:** Full server payload: `sub-opcode(1) + nChannel(1) + nResult(4)`. The "temp stat" set is `CTemporaryStatView::SetTemporary(nType=3, nID=1, tLeft=0x7FFFFFFF)` — this is **not** a `CharacterTemporaryStat`/`SecondaryStat` bit; it is a display-category-3 UI overlay entry in the buff bar specific to guild skill channel notifications. Results: 0=ResetTemporary; 1=activated; 2=already active; ≥3=`nResult-1` remaining. See Section 11, case 79.

---

## Table of Contents

1. [Constants & Limits](#1-constants--limits)
2. [Enums](#2-enums)
3. [Data Structures](#3-data-structures)
4. [Data Decode Functions](#4-data-decode-functions)
5. [Guild Skill System](#5-guild-skill-system)
6. [Passive Skill Data Application](#6-passive-skill-data-application)
7. [Shop Discount & Overcharge](#7-shop-discount--overcharge)
8. [Guild Mark System](#8-guild-mark-system)
9. [CWvsContext Guild Helpers](#9-cwvscontext-guild-helpers)
10. [CField Send Functions (Client→Server)](#10-cfield-send-functions-clientserver)
11. [CWvsContext::OnGuildResult (Server→Client)](#11-cwvscontextonguildresult-serverclient)
12. [CWvsContext::OnAllianceResult (Server→Client)](#12-cwvscontextonallianceresult-serverclient)
13. [Guild BBS System](#13-guild-bbs-system)
14. [Guild Boss Field](#14-guild-boss-field)
15. [Guild UI (Tab, Sort, Grade Windows)](#15-guild-ui-tab-sort-grade-windows)
16. [Packet Opcode Summary](#16-packet-opcode-summary)
17. [StringPool IDs Referenced](#17-stringpool-ids-referenced)

---

## 1. Constants & Limits

From `game_types.h`:

| Constant | Value (hex) | Value (dec) | Description |
|----------|-------------|-------------|-------------|
| `DB_GUILDMEMBER_MAX` | 0x64 | 100 | Maximum members per guild |
| `DB_GUILDMEMBER_MIN` | 0x0A | 10 | Minimum member capacity |
| `DB_GUILDNAME_MAX` | 0x0E | 14 | Max guild name length (chars) |
| `DB_GUILDNAME_MIN` | 0x04 | 4 | Min guild name length (chars) |
| `DB_GUILDGRADENAME_MAX` | 0x0A | 10 | Max grade name length (chars) |
| `DB_GUILDNOTICE_MAX` | 0x64 | 100 | Max **bulletin notice** text length (`m_sGuildNotice` / `SetGuildNotice`) — **not** BBS article content |
| `DB_ALLIANCE_MAX_MEMBER` | 0x05 | 5 | Max guilds per alliance |

> **No BBS named constants exist.** There are no `DB_GUILDBBS_*`, `BBS_*`, `BOARD_*`, or `ARTICLE_*` symbols anywhere in `game_types.h`. BBS limits are either inferred from UI setup code (see §13 BBS UI Limits) or enforced server-side only.
| `GUILD_GRADEMAX` | 0x05 | 5 | Maximum grade tiers |
| `GUILD_INIT_REQUIREDMEMBER` | 0x06 | 6 | Party members required to create guild |

---

## 2. Enums

### Guild Grades

```cpp
GUILD_NONE      = 0x0,   // Not in a guild
GUILD_MASTER    = 0x1,   // Guild Master
GUILD_SUBMASTER = 0x2,   // Jr. Master
GUILD_MEMBER1   = 0x3,   // Member Grade 1
GUILD_MEMBER2   = 0x4,   // Member Grade 2
GUILD_MEMBER3   = 0x5,   // Member Grade 3
```

### Guild Skill IDs

```cpp
GUILD_MESOUP              = 0x56C8CC0,  // 91000000 — Meso drop rate
GUILD_EXPERIENCEUP        = 0x56C8CC1,  // 91000001 — EXP rate
GUILD_DEFENCEUP           = 0x56C8CC2,  // 91000002 — Physical/Magic defense
GUILD_ATTNMAGUP           = 0x56C8CC3,  // 91000003 — Physical/Magic attack
GUILD_AGILITYUP           = 0x56C8CC4,  // 91000004 — Accuracy/Evasion
GUILD_BUSINESSEFFICENYUP  = 0x56C8CC5,  // 91000005 — Shop discount + overcharge
GUILD_REGULARSUPPORT      = 0x56C8CC6,  // 91000006 — Regular support buff
```

### Guild Request/Result Sub-Opcodes

```cpp
// ── Requests (Client → Server) ──
GuildReq_LoadGuild                = 0x00,
GuildReq_InputGuildName           = 0x01,
GuildReq_CheckGuildName           = 0x02,
GuildReq_CreateGuildAgree         = 0x03,
GuildReq_CreateNewGuild           = 0x04,
GuildReq_InviteGuild              = 0x05,
GuildReq_JoinGuild                = 0x06,
GuildReq_WithdrawGuild            = 0x07,
GuildReq_KickGuild                = 0x08,
GuildReq_RemoveGuild              = 0x09,
GuildReq_IncMaxMemberNum          = 0x0A,
GuildReq_ChangeLevel              = 0x0B,
GuildReq_ChangeJob                = 0x0C,
GuildReq_SetGradeName             = 0x0D,
GuildReq_SetMemberGrade           = 0x0E,
GuildReq_SetMark                  = 0x0F,
GuildReq_SetNotice                = 0x10,
GuildReq_InputMark                = 0x11,
GuildReq_CheckQuestWaiting        = 0x12,
GuildReq_CheckQuestWaiting2       = 0x13,
GuildReq_InsertQuestWaiting       = 0x14,
GuildReq_CancelQuestWaiting       = 0x15,
GuildReq_RemoveQuestCompleteGuild = 0x16,
GuildReq_IncPoint                 = 0x17,
GuildReq_IncCommitment            = 0x18,
GuildReq_SetQuestTime             = 0x19,
GuildReq_ShowGuildRanking         = 0x1A,
GuildReg_SetSkill                 = 0x1B,

// ── Results (Server → Client) ──
GuildRes_LoadGuild_Done                     = 0x1C,
GuildRes_CheckGuildName_Available           = 0x1D,
GuildRes_CheckGuildName_AlreadyUsed         = 0x1E,
GuildRes_CheckGuildName_Unknown             = 0x1F,
GuildRes_CreateGuildAgree_Reply             = 0x20,
GuildRes_CreateGuildAgree_Unknown           = 0x21,
GuildRes_CreateNewGuild_Done                = 0x22,
GuildRes_CreateNewGuild_AlreayJoined        = 0x23,
GuildRes_CreateNewGuild_GuildNameAlreayExist = 0x24,
GuildRes_CreateNewGuild_Beginner            = 0x25,
GuildRes_CreateNewGuild_Disagree            = 0x26,
GuildRes_CreateNewGuild_NotFullParty        = 0x27,
GuildRes_CreateNewGuild_Unknown             = 0x28,
GuildRes_JoinGuild_Done                     = 0x29,
GuildRes_JoinGuild_AlreadyJoined            = 0x2A,
GuildRes_JoinGuild_AlreadyFull              = 0x2B,
GuildRes_JoinGuild_UnknownUser              = 0x2C,
GuildRes_JoinGuild_Unknown                  = 0x2D,
GuildRes_WithdrawGuild_Done                 = 0x2E,
GuildRes_WithdrawGuild_NotJoined            = 0x2F,
GuildRes_WithdrawGuild_Unknown              = 0x30,
GuildRes_KickGuild_Done                     = 0x31,
GuildRes_KickGuild_NotJoined                = 0x32,
GuildRes_KickGuild_Unknown                  = 0x33,
GuildRes_RemoveGuild_Done                   = 0x34,
GuildRes_RemoveGuild_NotExist               = 0x35,
GuildRes_RemoveGuild_Unknown                = 0x36,
GuildRes_InviteGuild_BlockedUser            = 0x37,
GuildRes_InviteGuild_AlreadyInvited         = 0x38,
GuildRes_InviteGuild_Rejected               = 0x39,
GuildRes_AdminCannotCreate                  = 0x3A,
GuildRes_AdminCannotInvite                  = 0x3B,
GuildRes_IncMaxMemberNum_Done               = 0x3C,
GuildRes_IncMaxMemberNum_Unknown            = 0x3D,
GuildRes_ChangeLevelOrJob                   = 0x3E,
GuildRes_NotifyLoginOrLogout                = 0x3F,
GuildRes_SetGradeName_Done                  = 0x40,
GuildRes_SetGradeName_Unknown               = 0x41,
GuildRes_SetMemberGrade_Done                = 0x42,
GuildRes_SetMemberGrade_Unknown             = 0x43,
GuildRes_SetMemberCommitment_Done           = 0x44,
GuildRes_SetMark_Done                       = 0x45,
GuildRes_SetMark_Unknown                    = 0x46,
GuildRes_SetNotice_Done                     = 0x47,
GuildRes_InsertQuest                        = 0x48,
GuildRes_NoticeQuestWaitingOrder            = 0x49,
GuildRes_SetGuildCanEnterQuest              = 0x4A,
GuildRes_IncPoint_Done                      = 0x4B,
GuildRes_ShowGuildRanking                   = 0x4C,
GuildRes_GuildQuest_NotEnoughUser           = 0x4D,
GuildRes_GuildQuest_RegisterDisconnected    = 0x4E,
GuildRes_GuildQuest_NoticeOrder             = 0x4F,
GuildRes_Authkey_Update                     = 0x50,
GuildRes_SetSkill_Done                      = 0x51,
GuildRes_ServerMsg                          = 0x52,
```

### Alliance Request/Result Sub-Opcodes

```cpp
// ── Requests ──
AllianceReq_Create                = 0x00,
AllianceReq_Load                  = 0x01,
AllianceReq_Withdraw              = 0x02,
AllianceReq_Invite                = 0x03,
AllianceReq_Join                  = 0x04,
AllianceReq_UpdateMemberCountMax  = 0x05,
AllianceReq_Kick                  = 0x06,
AllianceReq_ChangeMaster          = 0x07,
AllianceReq_SetGradeName          = 0x08,
AllianceReq_ChangeGrade           = 0x09,
AllianceReq_SetNotice             = 0x0A,
AllianceReq_Destroy               = 0x0B,

// ── Results ──
AllianceRes_LoadDone              = 0x0C,
AllianceRes_LoadGuildDone         = 0x0D,
AllianceRes_NotifyLoginOrLogout   = 0x0E,
AllianceRes_CreateDone            = 0x0F,
AllianceRes_Withdraw_Done         = 0x10,
AllianceRes_Withdraw_Failed       = 0x11,
AllianceRes_Invite_Done           = 0x12,
AllianceRes_Invite_Failed         = 0x13,
AllianceRes_InviteGuild_BlockedByOpt  = 0x14,
AllianceRes_InviteGuild_AlreadyInvited = 0x15,
AllianceRes_InviteGuild_Rejected  = 0x16,
AllianceRes_UpdateAllianceInfo    = 0x17,
AllianceRes_ChangeLevelOrJob      = 0x18,
AllianceRes_ChangeMaster_Done     = 0x19,
AllianceRes_SetGradeName_Done     = 0x1A,
AllianceRes_ChangeGrade_Done      = 0x1B,
AllianceRes_SetNotice_Done        = 0x1C,
AllianceRes_Destroy_Done          = 0x1D,
AllianceRes_UpdateGuildInfo       = 0x1E,
```

### Packet Opcodes

```cpp
// Server → Client
LP_GuildBBS             = 0x3B,  // 59
LP_GuildRequest         = 0x42,  // 66
LP_GuildResult          = 0x43,  // 67
LP_AllianceResult       = 0x44,  // 68
LP_UserGuildNameChanged = 0xE4,  // 228
LP_UserGuildMarkChanged = 0xE5,  // 229

// Client → Server
CP_GuildRequest              = 0x95,  // 149
CP_GuildResult               = 0x96,  // 150
CP_AllianceRequest           = 0xA7,  // 167
CP_AllianceResult            = 0xA8,  // 168
CP_GuildBBS                  = 0xB3,  // 179
CP_RequestGuildBoardAuthKey  = 0x121, // 289
```

### UI Window IDs

```cpp
UI_GUILDBOARD    = 0x14,  // 20
UI_GUILDMAKEMARK = 0x24,  // 36
UI_GUILDMAKE     = 0x25,  // 37
UI_GUILDRANK     = 0x26,  // 38
UI_GUILDBBS      = 0x27,  // 39
```

### Chat & Menu

```cpp
CHAT_TYPE_GROUPGUILD     = 0x4,
CHAT_TYPE_GROUPALLIANCE  = 0x5,
CHATFILTER_GUILD         = 0x3,
CHATFILTER_ALLIANCE      = 0x4,
TAB_GUILD                = 0x3,
TAB_GUILDALLIANCE        = 0x4,
MENU_GUILD               = 0x11,
MENU_CHAT_GUILD          = 0x12,
MENU_CHAT_ALLIANCE       = 0x18,
ID_CHAT_TARGET_GUILD     = 0x4,
ID_CHAT_TARGET_ALLIANCE  = 0x5,
CG_Guild                 = 0x2,  // group message type
CG_Alliance              = 0x3,
```

### Game Options

```cpp
ID_CTRL_CHECKBOX_GUILDINVITE    = 0x3F1,
ID_CTRL_CHECKBOX_GUILDTALK      = 0x3F2,
ID_CTRL_CHECKBOX_ALLIANCEINVITE = 0x3F3,
ID_CTRL_CHECKBOX_ALLIANCETALK   = 0x3F4,
```

### Field Types

```cpp
FIELDTYPE_GUILDBOSS = 0x8,  // CField_GuildBoss subclass
```

---

## 3. Data Structures

### GUILDMEMBER (37 bytes)

```cpp
// game_types.h:24918 — sizeof = 0x25 (37 bytes), decoded via DecodeBuffer
struct __unaligned __declspec(align(1)) GUILDMEMBER
{
    char sCharacterName[13]; // 13-byte fixed string
    int  nJob;
    int  nLevel;
    int  nGrade;             // 1=Master, 2=Jr.Master, 3–5=Member grades
    int  bOnLine;
    int  nCommitment;        // guild contribution points
    int  nAllianceGrade;     // 1=AllianceMaster, 2=Sub, 3=Member
};
```

### GUILDDATA

```cpp
// game_types.h:24985
struct GUILDDATA
{
    int nGuildID;
    ZXString<char> sGuildName;
    ZArray<ZXString<char>> asGradeName;       // 5 grade name strings
    ZArray<unsigned long> adwCharacterID;      // member character IDs
    ZArray<GUILDMEMBER> aMemberData;           // parallel array with adwCharacterID
    int nMaxMemberNum;
    unsigned __int16 nMarkBg;
    unsigned __int8 nMarkBgColor;
    unsigned __int16 nMark;
    unsigned __int8 nMarkColor;
    ZXString<char> sNotice;
    int nPoint;                                // guild points (GP)
    int nAllianceID;
    int nLevel;                                // guild level (for sReqGL checks)
    ZMap<long, GUILDDATA::SKILLENTRY, long> mSkillRecord;   // skillID → SKILLENTRY
    ZArray<long> aSkillRecordOnlyID;           // flat array of skill IDs
};
```

### GUILDDATA::SKILLENTRY

```cpp
// game_types.h:24975
struct __cppobj __unaligned __declspec(align(2)) GUILDDATA::SKILLENTRY
{
    __int16 nLevel;                      // current skill level
    _FILETIME dateExpire;                // expiration timestamp (8 bytes)
    ZXString<char> strBuyCharacterName;  // who purchased/activated it
};
```

### GW_GuildSkillRecord (server-side record)

```cpp
// game_types.h:70715
struct __unaligned __declspec(align(1)) GW_GuildSkillRecord
{
    int nSkillID;                // guild skill ID
    __int16 nLevel;              // current skill level
    _FILETIME dateExpire;        // expiration timestamp
    char sBuyCharacterName[13];  // who purchased it
};
```

### ALLIANCEDATA

```cpp
// game_types.h:24998
struct ALLIANCEDATA
{
    int nAllianceID;
    ZXString<char> sAllianceName;
    ZArray<ZXString<char>> asGradeName;      // 5 alliance grade names
    ZArray<unsigned long> adwGuildID;         // member guild IDs
    int nMaxMemberNum;
    ZXString<char> sNotice;
};
```

### GUILDRANKING

```cpp
// game_types.h:49130
struct GUILDRANKING
{
    ZXString<char> sGuildName;
    int nPoint;
    int nMark;
    int nMarkColor;
    int nMarkBG;
    int nMarkBGColor;
};
```

### CWvsContext Guild Fields

```cpp
// game_types.h:24613 (within CWvsContext)
GUILDDATA m_guild;                          // own guild data
ZXString<unsigned short> m_sGuildBoardAuthkey;
unsigned int m_dwGuildBoardAuthkeyLastUpdated;
ALLIANCEDATA m_alliance;                    // own alliance data
ZArray<GUILDDATA> m_AllianceMember;         // other guilds in alliance
```

### PassiveSkillData (used by guild skill buffs)

```cpp
// game_types.h:24301
struct PassiveSkillData
{
    int nMHPr;       // Max HP rate %
    int nMMPr;       // Max MP rate %
    int nCr;         // Critical rate %
    int nCDMin;      // Critical damage min %
    int nACCr;       // Accuracy rate %
    int nEVAr;       // Evasion rate %
    int nAr;         // AR rate %
    int nEr;         // ER rate %
    int nPDDr;       // Physical defense rate %
    int nMDDr;       // Magic defense rate %
    int nPDr;        // Physical damage rate %
    int nMDr;        // Magic damage rate %
    int nDIPr;       // Damage ignore PDR %
    int nPDamr;      // Physical damage %
    int nMDamr;      // Magic damage %
    int nPADr;       // Physical attack rate %
    int nMADr;       // Magic attack rate %
    int nEXPr;       // EXP rate %
    int nIMPr;       // Ignore mob PDR %
    int nASRr;       // Abnormal status resist rate %
    int nTERr;       // Elemental resist rate %
    int nMESOr;      // Meso drop rate %
    int nPADx;       // Physical attack extra
    int nMADx;       // Magic attack extra
    int nIMDr;       // Ignore mob damage rate %
    int nPsdJump;    // Passive jump bonus
    int nPsdSpeed;   // Passive speed bonus
    int nOCr;        // Overcharge rate % (sell increase)
    int nDCr;        // Discount rate % (buy decrease)
    ZMap<long, ZRef<AdditionPsd>, long> mAdditionPsd;
};
```

### CONFIG_GAMEOPT (guild-relevant fields)

```cpp
// game_types.h:36426 (excerpt)
struct CONFIG_GAMEOPT
{
    // ...
    int bGameOpt_GuildInvite;
    int bGameOpt_GuildTalk;
    int bGameOpt_AllianceInvite;
    int bGameOpt_AllianceTalk;
    // ...
};
```

---

## 4. Data Decode Functions

### GUILDMEMBER::Decode

```c
// game_pseudocode.c:213501
void __thiscall GUILDMEMBER::Decode(GUILDMEMBER *this, CInPacket *iPacket)
{
    CInPacket::DecodeBuffer(iPacket, this, 0x25u);  // 37 bytes raw
}
```

### GUILDDATA::Decode

```c
// game_pseudocode.c:221057
void __thiscall GUILDDATA::Decode(GUILDDATA *this, CInPacket *iPacket)
{
    this->nGuildID = CInPacket::Decode4(iPacket);
    CInPacket::DecodeStr(iPacket, &this->sGuildName);

    // 5 grade names
    for (int i = 0; i < 5; i++)
        CInPacket::DecodeStr(iPacket, &this->asGradeName[i]);

    // Member arrays: count, then ALL character IDs contiguous, then ALL member data contiguous
    // ⚠ NOT interleaved — two bulk DecodeBuffer calls, each spanning the full count
    int nMemberCount = CInPacket::Decode1(iPacket);
    ZArray<unsigned long>::_Alloc(&this->adwCharacterID, nMemberCount);
    ZArray<GUILDMEMBER>::_Alloc(&this->aMemberData, nMemberCount);
    if (nMemberCount > 0) {
        CInPacket::DecodeBuffer(iPacket, this->adwCharacterID.a, 4 * nMemberCount);  // all IDs at once
        CInPacket::DecodeBuffer(iPacket, this->aMemberData.a, 37 * nMemberCount);   // all members at once
    }

    this->nMaxMemberNum = CInPacket::Decode4(iPacket);
    this->nMarkBg       = CInPacket::Decode2(iPacket);
    this->nMarkBgColor  = CInPacket::Decode1(iPacket);
    this->nMark         = CInPacket::Decode2(iPacket);
    this->nMarkColor    = CInPacket::Decode1(iPacket);
    CInPacket::DecodeStr(iPacket, &this->sNotice);
    this->nPoint        = CInPacket::Decode4(iPacket);
    this->nAllianceID   = CInPacket::Decode4(iPacket);
    this->nLevel        = CInPacket::Decode1(iPacket);

    // Guild skill records
    ZMap::RemoveAll(&this->mSkillRecord);
    ZArray::RemoveAll(&this->aSkillRecordOnlyID);
    int nSkillCount = CInPacket::Decode2(iPacket);
    for (int i = 0; i < nSkillCount; i++) {
        int nSkillID = CInPacket::Decode4(iPacket);
        GUILDDATA::SKILLENTRY entry;
        GUILDDATA::SKILLENTRY::Decode(&entry, iPacket);
        ZMap::Insert(&this->mSkillRecord, &nSkillID, &entry);
        ZArray<int>::InsertBefore(&this->aSkillRecordOnlyID, -1) = nSkillID;
    }
}
```

**Packet byte layout summary:**

| Field | Size | Type |
|-------|------|------|
| nGuildID | 4 | Decode4 |
| sGuildName | var | DecodeStr |
| asGradeName[0..4] | var×5 | DecodeStr |
| nMemberCount | 1 | Decode1 |
| all dwCharacterIDs (4 × count) | 4×N | DecodeBuffer — **one contiguous bulk read** |
| all GUILDMEMBER data (37 × count) | 37×N | DecodeBuffer — **one contiguous bulk read** |
| nMaxMemberNum | 4 | Decode4 |
| nMarkBg | 2 | Decode2 |
| nMarkBgColor | 1 | Decode1 |
| nMark | 2 | Decode2 |
| nMarkColor | 1 | Decode1 |
| sNotice | var | DecodeStr |
| nPoint | 4 | Decode4 |
| nAllianceID | 4 | Decode4 |
| nLevel | 1 | Decode1 |
| nSkillCount | 2 | Decode2 |
| per skill: nSkillID | 4 | Decode4 |
| per skill: SKILLENTRY | var | see below |

### GUILDDATA::SKILLENTRY::Decode

```c
// game_pseudocode.c:218827
void __thiscall GUILDDATA::SKILLENTRY::Decode(GUILDDATA::SKILLENTRY *this, CInPacket *iPacket)
{
    this->nLevel = CInPacket::Decode2(iPacket);              // 2 bytes: skill level
    CInPacket::DecodeBuffer(iPacket, &this->dateExpire, 8u); // 8 bytes: FILETIME
    CInPacket::DecodeStr(iPacket, &this->strBuyCharacterName);
}
```

| Field | Size | Type |
|-------|------|------|
| nLevel | 2 | Decode2 |
| dateExpire | 8 | DecodeBuffer |
| strBuyCharacterName | var | DecodeStr |

### ALLIANCEDATA::Decode

```c
// game_pseudocode.c:218841
void __thiscall ALLIANCEDATA::Decode(ALLIANCEDATA *this, CInPacket *iPacket)
{
    this->nAllianceID = CInPacket::Decode4(iPacket);
    CInPacket::DecodeStr(iPacket, &this->sAllianceName);
    for (int i = 0; i < 5; i++)
        CInPacket::DecodeStr(iPacket, &this->asGradeName[i]);
    int nGuildCount = CInPacket::Decode1(iPacket);
    // ⚠ bulk read — NOT individual Decode4s:
    ZArray<unsigned long>::_Alloc(&this->adwGuildID, nGuildCount);
    if (nGuildCount > 0)
        CInPacket::DecodeBuffer(iPacket, this->adwGuildID.a, 4 * nGuildCount);
    this->nMaxMemberNum = CInPacket::Decode4(iPacket);
    CInPacket::DecodeStr(iPacket, &this->sNotice);
}
```

### GUILDDATA Helper Functions

```c
// game_pseudocode.c:259630
int __thiscall GUILDDATA::FindIndex(GUILDDATA *this, unsigned int dwCharacterID)
{
    int count = this->adwCharacterID.a ? *(this->adwCharacterID.a - 1) : 0;
    for (int i = 0; i < count; i++) {
        if (this->adwCharacterID.a[i] == dwCharacterID)
            return i;
    }
    return -1;
}

// game_pseudocode.c:214645
int __thiscall GUILDDATA::GetCount(GUILDDATA *this)
{
    return this->adwCharacterID.a ? *(this->adwCharacterID.a - 1) : 0;
}

// game_pseudocode.c:~1263850
void __thiscall GUILDDATA::RemoveKey(GUILDDATA *this, int dwCharacterID)
{
    int idx = GUILDDATA::FindIndex(this, dwCharacterID);
    if (idx >= 0) {
        ZArray<unsigned long>::RemoveAt(&this->adwCharacterID, idx);
        ZArray<GUILDMEMBER>::RemoveAt(&this->aMemberData, idx);
    }
}
```

---

## 5. Guild Skill System

### Skill Identification

```c
// game_pseudocode.c:212701
BOOL __cdecl is_guild_skill(int nSkillID)
{
    return nSkillID > 0 && nSkillID / 10000 == 9100;
    // Guild skill IDs: 91000000–91009999
}
```

Additionally, a map field ID range check `nFieldID - 910000000 <= 0x16` is used for guild skill map detection (field IDs 910000000–910000022).

### Guild Skill ID Table

| Enum Name | Skill ID | Decimal | Primary Effect |
|-----------|----------|---------|----------------|
| `GUILD_MESOUP` | 0x56C8CC0 | 91000000 | `nMESOr` — Meso drop rate % |
| `GUILD_EXPERIENCEUP` | 0x56C8CC1 | 91000001 | `nEXPr` — EXP rate % |
| `GUILD_DEFENCEUP` | 0x56C8CC2 | 91000002 | `nPDDr`, `nMDDr` — Phys/Mag defense rate % |
| `GUILD_ATTNMAGUP` | 0x56C8CC3 | 91000003 | `nPADr`, `nMADr` — Phys/Mag attack rate % |
| `GUILD_AGILITYUP` | 0x56C8CC4 | 91000004 | `nACCr`, `nEVAr` — Accuracy/Evasion rate % |
| `GUILD_BUSINESSEFFICENYUP` | 0x56C8CC5 | 91000005 | `nDCr` (discount), `nOCr` (overcharge) |
| `GUILD_REGULARSUPPORT` | 0x56C8CC6 | 91000006 | Various support stats from WZ data |

> **Note:** The exact stat mapping per skill is determined by which `common` fields have non-zero formulas in `Skill.wz/910.img/skill/9100XXXX`. The code does NOT hardcode skill ID → stat — it reads whatever stats the WZ data defines for that skill entry.

### WZ Data Structure (Etc.wz/GuildSkill.img)

```
GuildSkill.img/
  {skillId}/
    level/
      {level}/               ← 1-based level index
        x           = 0      ← primary effect value (meaning depends on skill)
        y           = 0      ← secondary effect value
        time        = 0      ← buff duration in seconds (0=permanent)
        skill       = 0      ← linked skill ID (if grants a character skill)
        point       = 0      ← guild points required to activate
        money       = 0      ← meso cost to activate/maintain
        desc        = "..."  ← level description string
```

### Skill.wz Common Fields (per-level formulas)

Guild skills are also loaded via `CSkillInfo::LoadSkill` from `Skill.wz/9100.img/skill/9100XXXX` like any other skill. The image is named after the skill root (`91000000 / 10000 = 9100`), so the file is `9100.img` — **not** `910.img` (which holds only the 9 Admin/SuperGM skills `9101000`–`9101008`). Key common fields include:

| WZ Field | PassiveSkillData Target | Description |
|----------|------------------------|-------------|
| `sReqGL` | — | Min guild level formula (e.g. `"2*x+1"`) |
| `sDCr` | `nDCr` | Discount rate formula |
| `sOCr` | `nOCr` | Overcharge rate formula |
| `sMESOr` | `nMESOr` | Meso drop rate formula |
| `sEXPr` | `nEXPr` | EXP rate formula |
| `sPDDr` | `nPDDr` | Physical defense rate formula |
| `sMDDr` | `nMDDr` | Magic defense rate formula |
| `sPADr` | `nPADr` | Physical attack rate formula |
| `sMADr` | `nMADr` | Magic attack rate formula |
| `sACCr` | `nACCr` | Accuracy rate formula |
| `sEVAr` | `nEVAr` | Evasion rate formula |

### Getting Guild Skill Level

```c
// game_pseudocode.c:1264886
int __thiscall CWvsContext::GetGuildSkillLevel(CWvsContext *this, int nSkillID)
{
    if (nSkillID <= 0 || nSkillID / 10000 != 9100)
        return 0;
    GUILDDATA::SKILLENTRY se;
    GUILDDATA::SKILLENTRY *pEntry = ZMap<long,GUILDDATA::SKILLENTRY,long>::GetAt(
        &this->m_guild.mSkillRecord, &nSkillID, &se);
    if (!pEntry)
        return 0;
    return se.nLevel;
}

// Global wrapper — game_pseudocode.c:589977
int __cdecl _GetGuildSkillLevel(const CharacterData *cd, int nSkillID)
{
    return CWvsContext::GetGuildSkillLevel(
        TSingleton<CWvsContext>::ms_pInstance, nSkillID);
}
```

### Getting Guild Skill Array

```c
// game_pseudocode.c:1069092
ZArray<long> *__thiscall CWvsContext::GetGuildSkillArray(CWvsContext *this, ZArray<long> *result)
{
    result->a = 0;
    ZArray<long>::operator=(result, &this->m_guild.aSkillRecordOnlyID);
    return result;
}
```

### Skill Demand Checks (Quest/Skill Prerequisites)

When evaluating skill demands (quest prerequisites), guild skills are special-cased:

```c
// game_pseudocode.c:596510
v60 = is_guild_skill(nSkillID)
    ? _GetGuildSkillLevel(cd, nSkillID)        // from GUILDDATA.mSkillRecord
    : CSkillInfo::GetSkillLevel(..., nSkillID); // from personal mSkillRecord
```

### sReqGL — Required Guild Level

The `sReqGL` field specifies the minimum guild level to use/purchase a guild skill. Loaded from `Skill.wz/<skillID>/common`:

```c
// game_pseudocode.c:646686 (SKILLLEVELDATA::LoadCommonData)
ZXString<char>::Assign<unsigned short>(&v14->sReqGL, v302, -1);

// game_pseudocode.c:653135 (resolved at runtime)
v163 = SKILLLEVELDATA::GetParsedCommonData(this, sReqGL, nLevel);
// → evaluates formula string at given level, produces int
```

---

## 6. Passive Skill Data Application

### UpdatePassiveSkillData — Core Pipeline

Guild skills are applied as passive buffs **before** personal passive skills:

```c
// game_pseudocode.c:1083304
void __thiscall CUserLocal::UpdatePassiveSkillData(CUserLocal *this, int bDontSend)
{
    // Step 1: CLEAR all passive data
    PassiveSkillData::ClearData(this->m_pPassiveSkillData.p);

    // Step 2: GUILD SKILLS — applied first if guild exists
    if (CWvsContext->m_guild member count > 0) {
        ZArray<long> guildSkills = CWvsContext::GetGuildSkillArray(...);
        for (int i = 0; i < guildSkills.count; i++) {
            int nSkillID = guildSkills[i];
            int nLevel = CWvsContext::GetGuildSkillLevel(..., nSkillID);
            SKILLENTRY *pSkill = CSkillInfo::GetSkill(..., nSkillID);
            CUserLocal::SetPassiveSkillData(this, pSkill, nLevel);
        }
    }

    // Step 3: PERSONAL PASSIVE SKILLS — applied second
    for each skill in CharacterData->mSkillRecord {
        if (skill->nPsdSkill && level > 0)
            CUserLocal::SetPassiveSkillData(this, skill, level);
    }

    // Step 4: CLAMP values
    CUserLocal::RevisePassiveSkillData(this);
}
```

### SetPassiveSkillData — Stat Accumulation

All stats are **additively accumulated** from guild + personal sources:

```c
// game_pseudocode.c:1083067
void __thiscall CUserLocal::SetPassiveSkillData(
    CUserLocal *this, SKILLENTRY *pSkill, int nSLV)
{
    if (!pSkill || nSLV <= 0) return;
    SKILLLEVELDATA *ld = SKILLENTRY::GetLevelData(pSkill, nSLV);

    this->m_pPassiveSkillData.p->nMHPr     += ld->nMHPr;
    this->m_pPassiveSkillData.p->nMMPr     += ld->nMMPr;
    this->m_pPassiveSkillData.p->nCr       += ld->nCr;
    this->m_pPassiveSkillData.p->nCDMin    += ld->nCDMin;
    this->m_pPassiveSkillData.p->nACCr     += ld->nACCr;
    this->m_pPassiveSkillData.p->nEVAr     += ld->nEVAr;
    this->m_pPassiveSkillData.p->nAr       += ld->nAr;
    this->m_pPassiveSkillData.p->nEr       += ld->nEr;
    this->m_pPassiveSkillData.p->nPDDr     += ld->nPDDr;
    this->m_pPassiveSkillData.p->nMDDr     += ld->nMDDr;
    this->m_pPassiveSkillData.p->nPDr      += ld->nPDr;
    this->m_pPassiveSkillData.p->nMDr      += ld->nMDr;
    this->m_pPassiveSkillData.p->nDIPr     += ld->nDIPr;
    this->m_pPassiveSkillData.p->nPDamr    += ld->nPDamr;
    this->m_pPassiveSkillData.p->nMDamr    += ld->nMDamr;
    this->m_pPassiveSkillData.p->nPADr     += ld->nPADr;
    this->m_pPassiveSkillData.p->nMADr     += ld->nMADr;
    this->m_pPassiveSkillData.p->nEXPr     += ld->nEXPr;
    this->m_pPassiveSkillData.p->nIMPr     += ld->nIMPr;
    this->m_pPassiveSkillData.p->nASRr     += ld->nASRr;
    this->m_pPassiveSkillData.p->nTERr     += ld->nTERr;
    this->m_pPassiveSkillData.p->nMESOr    += ld->nMESOr;
    this->m_pPassiveSkillData.p->nPADx     += ld->nPADx;
    this->m_pPassiveSkillData.p->nMADx     += ld->nMADx;
    this->m_pPassiveSkillData.p->nIMDr     += ld->nIMDr;
    this->m_pPassiveSkillData.p->nPsdJump  += ld->nPsdJump;
    this->m_pPassiveSkillData.p->nPsdSpeed += ld->nPsdSpeed;
    this->m_pPassiveSkillData.p->nOCr      += ld->nOCr;
    this->m_pPassiveSkillData.p->nDCr      += ld->nDCr;
    // ... also processes AdditionPsd map for per-weapon-type bonuses ...
}
```

### RevisePassiveSkillData — Clamping

```c
// game_pseudocode.c:1066549
void __thiscall CUserLocal::RevisePassiveSkillData(CUserLocal *this)
{
    // nMESOr: clamp [0, 100]
    this->m_pPassiveSkillData.p->nMESOr = clamp(nMESOr, 0, 100);
    // nOCr: clamp [0, 50]
    this->m_pPassiveSkillData.p->nOCr = clamp(nOCr, 0, 50);
    // nDCr: clamp [0, 50]
    this->m_pPassiveSkillData.p->nDCr = clamp(nDCr, 0, 50);
}
```

**Max caps:** `nMESOr` = 100%, `nOCr` = 50%, `nDCr` = 50%.

### SetPassiveSkillDataForced — Debug/GM Command

Allows forcibly setting a specific passive field via slash command. Requires guild member status:

```c
// game_pseudocode.c:1071352
// Triggered via OpCode 151 (0x97), sub-opcode 0x3D
// Example: /setpsd discountr 30
// Supported field names: mhpr, mmpr, cr, criticaldamage, accr, evar, ar, er,
//   pddr, mddr, pdr, mdr, damr, padr, madr, expr, ignoremobpdpr,
//   asrr, terr, mesor, padx, madx, ignoremobdamr, psdjump, psdspeed,
//   overcharger, discountr
```

### End-to-End Guild Skill Flow

1. **Loading from WZ**: Guild skills are loaded by `CSkillInfo::LoadSkill` from `Skill.wz/9100.img/skill/9100XXXX`. Their `common` properties (`sReqGL`, `sDCr`, `sOCr`, etc.) are loaded as formula strings.

2. **Guild data arrives**: `GUILDDATA::Decode` reads skill levels from server. Each skill has ID, level, expiration, buyer name. Stored in `CWvsContext::m_guild.mSkillRecord` (map) and `aSkillRecordOnlyID` (array).

3. **Passive update cycle**: `CUserLocal::UpdatePassiveSkillData` clears passive data, then:
   - Iterates `aSkillRecordOnlyID` → gets level via `GetGuildSkillLevel` → gets `SKILLENTRY` via `CSkillInfo::GetSkill` → calls `SetPassiveSkillData` which reads `SKILLLEVELDATA` and accumulates into `PassiveSkillData`.
   - Then processes personal passive skills the same way.

4. **Clamping**: `RevisePassiveSkillData` caps `nMESOr` to 100%, `nOCr` and `nDCr` to 50%.

5. **Skill activation packet**: When a guild skill is purchased/activated, server sends `OnGuildResult` case 81. Client decodes skill entry and inserts into `m_guild.mSkillRecord`.

6. **Demand checks**: Quest/skill prerequisites that reference guild skills use `is_guild_skill()` to branch to `_GetGuildSkillLevel` instead of personal level check.

---

## 7. Shop Discount & Overcharge

### GetDiscountPriceByGuildSkill (nDCr)

```c
// game_pseudocode.c:630603
int __thiscall CShopDlg::GetDiscountPriceByGuildSkill(CShopDlg *this, int nPrice)
{
    PassiveSkillData *psd = CUserLocal::GetPassiveSkillData(...);
    if (!psd || psd->nDCr <= 0)
        return 0;  // No discount

    // Formula: discountedPrice = (100 - nDCr) * nPrice / 100.0
    double dResult = (double)(100 - psd->nDCr) * (double)nPrice / 100.0;
    // Round: fractional digit >= 5 rounds up
    if ((int)(10.0 * dResult) % 10 >= 5) dResult += 1.0;
    return min(nPrice, (int)dResult);
}
```

### GetOverchargePrice (nOCr)

```c
// game_pseudocode.c:630668
int __thiscall CShopDlg::GetOverchargePrice(CShopDlg *this, int nPrice)
{
    PassiveSkillData *psd = CUserLocal::GetPassiveSkillData(...);
    if (!psd || psd->nOCr <= 0)
        return 0;  // No overcharge

    // Formula: overchargedPrice = (nOCr + 100) * nPrice / 100.0
    double dResult = (double)(psd->nOCr + 100) * (double)nPrice / 100.0;
    if ((int)(10.0 * dResult) % 10 >= 5) dResult += 1.0;
    return max(nPrice, (int)dResult);
}
```

### GetDiscountPrice — Final Logic

```c
// game_pseudocode.c:631078
int __thiscall CShopDlg::GetDiscountPrice(CShopDlg *this, int nPrice, int nItemID)
{
    int byItem  = CShopDlg::GetDiscountPriceByItem(this, nPrice, nItemID);
    int byGuild = CShopDlg::GetDiscountPriceByGuildSkill(this, nPrice);

    // Normalize: if >= original price or negative, treat as no discount
    if (byItem >= nPrice || byItem < 0)   byItem = 0;
    if (byGuild >= nPrice || byGuild < 0) byGuild = 0;

    // Prefer item-specific discount if cheaper; else use guild discount
    if (byItem && (!byGuild || byItem < byGuild))
        return byItem;
    return byGuild;  // 0 = no discount
}
```

---

## 8. Guild Mark System

### CWvsContext::GetGuildMarkCanvas

```c
// game_pseudocode.c:1264441
// Composites a 17×17 guild mark canvas from mark/background assets
IWzCanvas *__thiscall CWvsContext::GetGuildMarkCanvas(CWvsContext *this, IWzCanvas **ppResult)
{
    int nMarkBg = this->m_guild.nMarkBg;
    int nMark = this->m_guild.nMark;
    if (!nMarkBg && !nMark) { *ppResult = NULL; return NULL; }

    // Load background: Etc.wz/GuildMark.img/%04d (nMarkBg)
    // Load foreground: Etc.wz/GuildMark.img/%04d (nMark)
    // Create 17×17 output canvas
    // Composite: draw background tinted with nMarkBgColor, overlay mark tinted with nMarkColor
    // Uses ARGB color application
    return composited_canvas;
}
```

### Mark Data Source

Guild marks use `Etc.wz/GuildMark.img/{id}` canvases:
- `nMarkBg` (uint16): background mark ID
- `nMarkBgColor` (uint8): background tint color index
- `nMark` (uint16): foreground mark ID
- `nMarkColor` (uint8): foreground tint color index

### CField::SendSetGuildMarkMsg

```c
// game_pseudocode.c:262052
void __thiscall CField::SendSetGuildMarkMsg(CField *this,
    unsigned short nMarkBg, char nMarkBgColor,
    unsigned short nMark, char nMarkColor)
{
    COutPacket oPacket(149);              // Guild request header
    COutPacket::Encode1(&oPacket, 0x0F); // sub-opcode 15 = set mark
    COutPacket::Encode2(&oPacket, nMarkBg);
    COutPacket::Encode1(&oPacket, nMarkBgColor);
    COutPacket::Encode2(&oPacket, nMark);
    COutPacket::Encode1(&oPacket, nMarkColor);
    CClientSocket::SendPacket(..., &oPacket);
}
```

### CSetGuildMarkDlg States

```cpp
enum CSetGuildMarkDlg::<unnamed_tag> : __int32
{
    CREATE_DIALOG        = 0x0,
    SHOW_ANIMATION_START = 0x1,
    SHOW_ANIMATION_END   = 0x2,
    SHOW_MESSAGE_START   = 0x3,
    SHOW_MESSAGE_END     = 0x4,
};
```

---

## 9. CWvsContext Guild Helpers

### Authority Checks

```c
// game_pseudocode.c:117012
int __thiscall CWvsContext::AmIGuildMaster(CWvsContext *this)
{
    return this->m_guild.nGuildID
        && *this->m_guild.adwCharacterID.a == this->m_dwCharacterId;
    // Master is always index 0 in the member array
}

int __thiscall CWvsContext::AmIAllianceMaster(CWvsContext *this)
{
    if (!this->m_guild.nAllianceID) return 0;
    int idx = GUILDDATA::FindIndex(&this->m_guild, this->m_dwCharacterId);
    if (idx < 0) return 0;
    return this->m_guild.aMemberData.a[idx].nAllianceGrade == 1;
}

int __thiscall CWvsContext::AmIAllianceSubMaster(CWvsContext *this)
{
    if (!this->m_guild.nAllianceID) return 0;
    int idx = GUILDDATA::FindIndex(&this->m_guild, this->m_dwCharacterId);
    if (idx < 0) return 0;
    return this->m_guild.aMemberData.a[idx].nAllianceGrade == 2;
}
```

### Info Getters

```c
ZXString<char> *CWvsContext::GetGuildName(...)       // returns m_guild.sGuildName
ZXString<char> *CWvsContext::GetGuildNotice(...)      // returns m_guild.sNotice
ZXString<char> *CWvsContext::GetAllianceName(...)     // returns m_alliance.sAllianceName
ZXString<char> *CWvsContext::GetAllianceNotice(...)   // returns m_alliance.sNotice

int CWvsContext::GetGuildMemberNum(...)               // returns member count
int CWvsContext::GetGuildMemberGrade(dwCharacterID)   // FindIndex → grade
GUILDMEMBER *CWvsContext::GetGuildMemberDataByIdx(nIdx)
unsigned int CWvsContext::GetGuildMemberIDByName(sName)
void CWvsContext::GetGuildMemberNameByID(dwCharacterID, *pResult)

ZXString<char> *CWvsContext::GetGuildGradeName(nGrade)   // asGradeName[nGrade - 1]
ZXString<char> *CWvsContext::GetAllianceGradeName(nGrade)
```

### Grade Limits

```c
// game_pseudocode.c:1263420
int CWvsContext::GetGuildMaxGradeNum(...)
{
    // Returns 3, 4, or 5 depending on how many grade names are non-empty
    if (asGradeName[4] empty) {
        if (asGradeName[3] empty) return 3;
        return 4;
    }
    return 5;
}
// Same pattern for GetAllianceMaxGradeNum
```

### Member Lookup

```c
// game_pseudocode.c:1263557
int CWvsContext::IsGuildMemberExist(int nGrade)
{
    for each member: if (member.nGrade == nGrade) return 1;
    return 0;
}

// game_pseudocode.c:1263927
unsigned int CWvsContext::GetGuildMemberIDByName(ZXString<char> sName)
{
    for each member: if (name matches) return characterID;
    return 0;
}

// game_pseudocode.c:1263982
void CWvsContext::GetGuildMemberNameByID(unsigned int dwCharacterID, ZXString<char> *pResult)
{
    int idx = FindIndex(dwCharacterID);
    if (idx >= 0) *pResult = member[idx].sCharacterName;
}
```

### Alliance Member Management

```c
// game_pseudocode.c:1266556
void CWvsContext::UpdateAllianceMemberInfo(GUILDDATA *gd)
{
    if (gd->nGuildID == own guild ID) {
        GUILDDATA::operator=(&m_guild, gd);
        // remove from AllianceMember if present
    } else {
        // find in AllianceMember → update, or insert new
    }
}
```

### ShowGuildInfo (Debug Display)

```c
// game_pseudocode.c:1264312
// For each member: logs "name [grade] Lv.N job online/offline" to chat (type 12)
```

### Guild NPC Dialog

```c
// game_pseudocode.c:1264151
void CWvsContext::GuildNPCSay(ZArray<ZXString<char>> *aText, int nNpcID)
{
    // Creates CUtilDlgEx dialog with NPC portrait (nNpcID = 2010007 typically)
    // Supports next/previous navigation through text array
}
```

---

## 10. CField Send Functions (Client→Server)

All guild request packets use COutPacket header **149** (`CP_GuildRequest`).

> **Dead opcodes (never sent by V95 client):** `0x04` (GuildReq_CreateNewGuild), `0x09`
> (GuildReq_RemoveGuild), `0x1B` (GuildReg_SetSkill) — none of these appear in any
> `COutPacket(149)` construction in the binary. All three are NPC-script-driven
> server-side operations. Do **not** wait for them from clients.

| Sub-opcode | Function | Source | Packet Layout |
|-----------|----------|--------|---------------|
| 0x00 | (acknowledge join) | `OnGuildResult` case 41 | Encode1(0) only |
| 0x02 | `InputGuildName` | `CField::InputGuildName` | Encode1(2) + EncodeStr(guildName) |
| 0x05 | `SendInviteGuildMsg` | `CField::SendInviteGuildMsg` | Encode1(5) + EncodeStr(targetName) |
| 0x06 | (accept guild invite) | `CUIFadeYesNo::OnButtonClicked` m_nType=8 | Encode1(6) + Encode4(inviterCharID) + Encode4(myCharID) |
| 0x07 | `SendWithdrawGuildMsg` | `CField::SendWithdrawGuildMsg` | Encode1(7) + Encode4(myCharID) + EncodeStr(myCharName) |
| 0x08 | `SendKickGuildMsg` | `CField::SendKickGuildMsg` | Encode1(8) + Encode4(targetCharID) + EncodeStr(targetName) |
| 0x0D | `SendSetGradeNameMsg` | `CField::SendSetGradeNameMsg` | Encode1(13) + EncodeStr×5 |
| 0x0E | `SendSetMemberGradeMsg` | `CField::SendSetMemberGradeMsg` | Encode1(14) + Encode4(charID) + Encode1(grade) |
| 0x0F | `SendSetGuildMarkMsg` | `CField::SendSetGuildMarkMsg` | Encode1(15) + Encode2(markBg) + Encode1(bgColor) + Encode2(mark) + Encode1(markColor) |
| 0x10 | `SendSetGuildNoticeMsg` | `CField::SendSetGuildNoticeMsg` | Encode1(16) + EncodeStr(notice) |
| 0x20 | `SendCreateGuildAgreeMsg` | `CField::SendCreateGuildAgreeMsg` | Encode1(32) + Encode4(myCharID) + Encode1(bAgree) |

Guild **response** packets use header **150** (`CP_GuildResult`):

| Sub-opcode | Context | Packet Layout |
|-----------|---------|---------------|
| 55 | Decline invite | Encode1(55) + EncodeStr(inviterName) + EncodeStr(myName) |
| 56 | Already invited | Encode1(56) + EncodeStr(inviterName) + EncodeStr(myName) |

### Validation Rules (Client-Side)

- **Invite**: Requires guild to exist, caller grade ≤ 2 (master/jr.master), member count < max.
- **Withdraw**: Shows YesNo confirmation (StringPool 0x164). Guild master cannot withdraw (error 0x16C).
- **Kick**: Jr.Master cannot kick other Jr.Masters. Cannot kick the guild master.

---

## 11. CWvsContext::OnGuildResult (Server→Client)

**Line 1265283** — Main guild packet handler. First byte = sub-opcode.

### Complete Switch Case Map

| Case | Name | Behavior |
|------|------|----------|
| 1 | InputGuildName | Prompts guild name input (create flow) |
| 3 | CreateGuildAgree | **Boss:** no bytes read — NPC 2010007 says start dialog. **Non-boss:** `Decode4(partyID)` guard + `DecodeStr(inviterName)` + `DecodeStr(guildName)` → shows `CCreateGuildAgreeDlg`; result fires `SendCreateGuildAgreeMsg` (sub-op 0x20) |
| 5 | GuildInvite | Payload: `inviterCharID(4) + inviterName(str) + inviterLevel(4) + inviterJob(4)`. Level decoded **before** job (`CreateGuildInvite` signature: `(sInviter, nLevel, nJobCode, dwInviterID, bGuildOpt)`). Shows `CUIFadeYesNo::CreateGuildInvite`; auto-declines (sends CP_GuildResult 55 or 56) if blacklisted or already-invited. |
| 17 | SetGuildMark | Opens `CSetGuildMarkDlg` modal for guild master |
| 28 | LoadGuild | Always calls `GUILDDATA::Clear`. `Decode1(hasData)`: if hasData=1 (non-zero): `GUILDDATA::Decode`; if hasData=0: guild stays empty. Either way: `UpdatePassiveSkillData`; if `nAllianceID≠0` (only possible when hasData=1): sends AllianceReq_Load (opcode 167 sub 1); removes own guild from `m_AllianceMember` array. |
| 30 | GuildNameInUse | Shows "name in use" NPC dialog, re-prompts `InputGuildName` |
| 33 | GuildCreateError | NPC dialog (StringPool 0xCF3) |
| 34 | GuildCreated | Full guild decode; master sees "created" NPC dialog; members see chat message (0x15D) |
| 35 | Error | Chat log (0x169) |
| 37 | Error | Chat log (0x16A) |
| 38 | Error | NPC dialog (0xCEE) |
| 40 | Error | NPC dialog (0xCF4) |
| 41 | MemberJoin | Decodes `guildID(4)` + `charID(4)`. **Two different server payloads:** (a) To the **joining player** (charID == self): server sends only these 8 bytes — client shows chat (0x167) and sends CP_GuildRequest sub=0 (bare, no payload). (b) To **existing guild members** (guildID == own): server appends `GUILDMEMBER(37)` — client calls `GUILDMEMBER::Decode` and inserts + shows chat (0x168). |
| 42 | Error | Chat log (0x169) |
| 43 | InviteNotAllowed | Chat log (0x16D) |
| 44 | Error | Chat log (0x177) |
| 46 | MemberLeave | Decodes guildID + charID + charName. If self: clears guild, destroys CWndGuildBoard. If other: removes from arrays |
| 47 | WithdrawError | Chat log (0x16B) |
| 49 | MemberExpelled | Same as 46 but: self → "expelled" (0x1A8C), other → "kicked" (0x160) |
| 50 | ExpelError | Chat log (0x16B) |
| 52 | GuildDisbanded | Payload: `Decode4(guildID)` guard only. Master: NPC dialog (0xCF1) then `GUILDDATA::Clear`. Members: chat (0x166) then `GUILDDATA::Clear` |
| 54 | DisbandError | NPC dialog (0xCF5) |
| 55 | InviteSent | Decodes target name; chat (0x15B) |
| 56 | InviteRejected | Decodes target name; chat (0xACF) |
| 57 | InviteDenied | Decodes target name; chat (0x15C) |
| 58 | Error | Chat log (0x174) |
| 60 | CapacityChanged | Decodes guildID + new nMaxMemberNum; master NPC dialog (0xCF2) |
| 61 | CapacityError | NPC dialog (0xCF6) |
| 62 | MemberLevelJobChanged | Decodes guildID + charID + nLevel + nJob; updates member |
| 63 | MemberOnlineChanged | Decodes guildID + charID + bOnLine; online → user alarm |
| 64 | SetGradeNames | Decodes guildID + 5 strings; master notice (0xD01) |
| 66 | MemberGradeChanged | Decodes guildID + charID + nGrade; chat w/ member+grade name; if self destroys CWndGuildBoard |
| 69 | MarkChanged | Decodes guildID + markBg(2) + bgColor(1) + mark(2) + color(1); master notice (0xD2D) |
| 71 | NoticeChanged | Decodes guildID + notice string; chat (0xD36) |
| 75 | PointLevelChanged | Decodes guildID + nPoint + nLevel |
| 76 | GuildRanking | Decodes count + entries; shows `CGuildRankDlg` modal |
| 77 | SkillError | Chat log (0xDFD) |
| 78 | SkillError | Chat log (0xDFE) |
| 79 | GuildSkillResult | `nChannel(1)` + `nResult(4)`. 0=`ResetTemporary(nType=3,nID=1)`; 1=activated (SP 0xDFF); 2=already-active (SP 0xE00); ≥3=`nResult−1` remaining (SP 0xE01). `nType=3` is `CTemporaryStatView` display-category (NOT `SecondaryStat`/`TEMPORARY_STAT`); `nID=1`, `tLeft=0x7FFFFFFF`. Broadcast to guild members in `nChannel`. |
| 80 | BoardAuthKey | `sAuthKey(STR, narrow)`. Client decodes via `DecodeStr` then converts to UTF-16 with `ZStrUtil::_Conv`; stores in `m_sGuildBoardAuthkey` (`ZXString<unsigned short>`); timestamps `m_dwGuildBoardAuthkeyLastUpdated`. **Server PUSH only** — `CP_RequestGuildBoardAuthKey (0x121)` is dead, never sent. |
| 81 | SkillEntryUpdate | Decodes guildID + nSkillID + SKILLENTRY; inserts into mSkillRecord |
| 82 | GenericMsg | Decode1 flag: 1=decode+show string; 0=show StringPool 0x176 |
| default | Unknown | Shows StringPool 0x176 |

### Common Post-Processing (LABEL_201)

After most cases:
1. If `CUIUserList` exists and tab==3 (guild): `CUIUserList::ResetInfo`
2. If `CUserLocal` exists: `CUserLocal::RedrawGuildNameTag`
3. If `CUIUserList` exists and tab==4 (alliance): `CUIUserList::ResetInfo`

---

## 12. CWvsContext::OnAllianceResult (Server→Client)

**Line 1266612** — Alliance packet handler.

### Complete Switch Case Map

| Case | Name | Description |
|------|------|-------------|
| 3 | AllianceInvite | Decodes senderID + inviter name + guild name. Auto-responds if blacklisted else `CUIFadeYesNo::CreateAllianceInvite` |
| 12 | LoadAlliance | `ALLIANCEDATA::Clear` + conditional `ALLIANCEDATA::Decode` |
| 13 | LoadAllianceMembers | Decodes count + GUILDDATA entries; `UpdateAllianceMemberInfo` each |
| 14 | MemberOnlineChanged | Decodes allianceID+guildID+charID+bOnLine; user alarm if online |
| 15 | AllianceInfoFull | Full alliance decode + guild data per member guild |
| 16 | GuildLeaveAlliance | Full alliance+expelled guildID. Self: clears, re-decodes own guild, updates passives (0x1897/0x1898). Other: updates, removes |
| 18 | GuildJoinAlliance | Alliance data + joining guildID. If own: decodes self guild, msg (0x1881). If other: adds (0x1882) |
| 23 | AllianceUpdated | Decodes ALLIANCEDATA; updates if matches own alliance |
| 24 | MemberLevelJobChanged | Decodes allianceID+guildID+charID+level+job; updates AllianceMember |
| 25 | MasterTransfer | Decodes allianceID+oldID+newID; sets old→grade2, new→grade1 |
| 26 | GradeNamesChanged | Decodes allianceID + 5 grade strings |
| 27 | MemberGradeChanged | Decodes charID + newGrade; updates own/alliance members |
| 28 | NoticeChanged | Decodes allianceID + notice |
| 29 | AllianceDisbanded | Clears AllianceMember, alliance, sets all nAllianceGrade=3, clears nAllianceID |
| 30 | GuildDataUpdate | Decodes allianceID+guildID+GUILDDATA; `UpdateAllianceMemberInfo` |

### Post-Processing (LABEL_128)

If `CUIUserList` exists and tab==4: `CUIUserList::ResetInfo`.

---

## 13. Guild BBS System

Two entirely separate BBS implementations exist in V95:

| Class | UI Type | Opcode | Notes |
|-------|---------|--------|-------|
| `CUIGuildBBS` | In-game native | CP/LP\_GuildBBS (179 / 59) | `TSingleton`, no web view |
| `CWndGuildBoard` | IE embedded web view | Uses auth key from case 80 | Not opened by guild tab buttons in V95 |

Guild tab button ID `0x7FA` (`CTabGuild::OnButtonClicked`) calls `CWvsContext::UI_Toggle(0x27, -1)` to open `CUIGuildBBS`. The `CWndGuildBoard` construction path is absent from the V95 binary; only its destruction is reachable (button 1000 in `CUIUserList::OnButtonClicked`).

### LP_GuildBBS Dispatch (server→client)

```c
// game_pseudocode.c:1214917
void CWvsContext::OnGuildBBSPacket(CWvsContext *this, CInPacket *iPacket)
{
    if (TSingleton<CUIGuildBBS>::ms_pInstance)
        CUIGuildBBS::OnGuildBBSPacket(ms_pInstance, iPacket);
}

// game_pseudocode.c:806409
void CUIGuildBBS::OnGuildBBSPacket(CUIGuildBBS *this, CInPacket *iPacket)
{
    switch (CInPacket::Decode1(iPacket)) {
        case 6: CUIGuildBBS::OnLoadListResult(this, iPacket); break;
        case 7: CUIGuildBBS::OnViewEntryResult(this, iPacket); break;
        case 8: CUIGuildBBS::OnEntryNotFound(this);           break; // no iPacket arg
    }
}
```

### CP_GuildBBS Sub-Opcodes (client→server, `game_pseudocode.c:802961+`)

All use `COutPacket(179)` + `Encode1(sub)`.

| Sub | Handler | Additional payload (after sub byte) |
|-----|---------|--------------------------------------|
| 0 | `OnRegister` — write / modify | `Encode1(bModify)` + *if bModify:* `Encode4(nCurEntryID)` + `Encode1(bNotice)` + `EncodeStr(sTitle)` + `EncodeStr(sText)` + `Encode4(nEmoticonID)` |
| 1 | `OnDelete` | `Encode4(nCurEntryID)` |
| 2 | `SendLoadListRequest` | `Encode4(nEntryListStart)` (0-based; 10 entries/page) |
| 3 | `SendViewEntryRequest` | `Encode4(nViewRequestEntryID)` then immediately also fires sub 2 |
| 4 | `OnComment` — add comment | `Encode4(nCurEntryID)` + `EncodeStr(sComment)` |
| 5 | `OnCommentDelete` | `Encode4(nCurEntryID)` + `Encode4(m_nSN)` |

### LP_GuildBBS Case 6: `OnLoadListResult` (`game_pseudocode.c:803710`)

> ⚠ Wire decode order differs from `ENTRYLIST` struct field order: `nCharacterID` is decoded before `sTitle`; `nEmoticon` is decoded before `nComments`.

| Field | Size | Notes |
|-------|------|-------|
| `has_notice` | 1 | 1 if a pinned notice exists |
| *if has\_notice:* | | |
| `nEntryID` | 4 | Notice entry ID |
| `nCharacterID` | 4 | Author character ID — decoded **before** sTitle |
| `sTitle` | STR | Notice title |
| `ftDate` | 8 | `_FILETIME` |
| `nEmoticon` | 4 | Emoticon ID — decoded **before** nComments |
| `nComments` | 4 | Comment count on notice |
| `nEntryListTotalCount` | 4 | Total articles on the board |
| `nPageEntryCount` | 4 | Entries in this page (≤10) |
| *for each page entry (same layout):* | | |
| `nEntryID` | 4 | |
| `nCharacterID` | 4 | ← before sTitle |
| `sTitle` | STR | |
| `ftDate` | 8 | `_FILETIME` |
| `nEmoticon` | 4 | ← before nComments |
| `nComments` | 4 | |

### LP_GuildBBS Case 7: `OnViewEntryResult` (`game_pseudocode.c:805097`)

> ⚠ Wire order: date fields decoded before adjacent string fields in both `CURENTRY` and `COMMENT`.

| Field | Size | Notes |
|-------|------|-------|
| `nCurEntryID` | 4 | Entry being viewed |
| `nCurCharacterID` | 4 | Author character ID |
| `ftCurDate` | 8 | `_FILETIME` — decoded **before** title/text strings |
| `sCurTitle` | STR | |
| `sCurText` | STR | |
| `nEmoticon` | 4 | |
| `nCommentCount` | 4 | |
| *for each comment:* | | |
| `m_nSN` | 4 | Comment serial number |
| `m_nCharacterID` | 4 | Commenter character ID |
| `m_ftDate` | 8 | `_FILETIME` — decoded **before** m\_sComment |
| `m_sComment` | STR | |

### LP_GuildBBS Case 8: `OnEntryNotFound`

No payload fields. Server sends only the sub-opcode byte (8). `CUIGuildBBS::OnEntryNotFound(void)` takes no `CInPacket` argument — it displays StringPool **0xEC7** and clears `m_CurEntry`.

### Auth Key: Case 80 (`GuildRes_Authkey_Update`)

`CP_RequestGuildBoardAuthKey (0x121 = 289)` **is a dead opcode** — `COutPacket(289)` is never constructed in the V95 binary. The server pushes the auth key proactively:

```c
// game_pseudocode.c:~1266470 — case 80 in OnGuildResult
// Server payload: sub-opcode(1) [=80] + sAuthKey(STR, narrow char)
DecodeStr(iPacket, ...);                        // narrow char string
ZStrUtil::_Conv(&m_sGuildBoardAuthkey, ...);    // → ZXString<unsigned short> (UTF-16)
m_dwGuildBoardAuthkeyLastUpdated = ZAPI.timeGetTime();
```

### `CUIGuildBBS` Struct (`game_types.h:32396`)

```cpp
struct __cppobj CUIGuildBBS : CUIWnd, TSingleton<CUIGuildBBS>
{
    int                                     m_bViewEntry;
    int                                     m_nViewRequestEntryID;
    ZRef<CUIGuildBBS::CURENTRY>             m_CurEntry;
    int                                     m_nEntryListStart;
    ZArray<ZRef<CUIGuildBBS::ENTRYLIST>>    m_aEntryList;
    ZRef<CUIGuildBBS::ENTRYLIST>            m_Notice;
    int                                     m_bWriteTextBox;
    int                                     m_nEmoticonIdx;
    int                                     m_nEmoticonID;
    ZRef<CCtrlButton>                       m_pBtWrite;
    ZRef<CCtrlButton>                       m_pBtWriteNotice;
    ZRef<CCtrlButton>                       m_pBtDelete;
    ZRef<CCtrlButton>                       m_pBtModify;
    ZRef<CCtrlButton>                       m_pBtRegister;
    ZRef<CCtrlButton>                       m_pBtCancel;
    ZRef<CCtrlButton>                       m_pBtComment;
    ZRef<CCtrlButton>                       m_pBtCommentDelete[4]; // up to 4 shown
    ZRef<CCtrlButton>                       m_pBtExit;
    ZRef<CCtrlButton>                       m_pBtEmoticonLeft;
    ZRef<CCtrlButton>                       m_pBtEmoticonRight;
    ZRef<CCtrlEdit>                         m_pEditTitle;
    ZRef<CCtrlMLEdit>                       m_pEditText;
    ZRef<CCtrlEdit>                         m_pEditComment;
    ZRef<CCtrlScrollBar>                    m_pSBComment;
    ZRef<CCtrlScrollBar>                    m_pSBEdit;
    ZRef<CCtrlSelector>                     m_pSelector;            // page selector
    ZArray<long>                            m_aCashEmoticonID;
    // ... canvas and layer fields ...
};
```

### `CUIGuildBBS::ENTRYLIST` (`game_types.h:32240`, 0x28 = 40 bytes)

```cpp
struct __cppobj CUIGuildBBS::ENTRYLIST : ZRefCounted  // vtbl+nRef+pPrev = 12 bytes
{
    int             nEntryID;       // offset 12
    ZXString<char>  sTitle;         // offset 16
    int             nCharacterID;   // offset 20
    _FILETIME       ftDate;         // offset 24 (8 bytes)
    int             nComments;      // offset 32
    int             nEmoticon;      // offset 36
};
```

### `CUIGuildBBS::CURENTRY` (`game_types.h:32210`, 0x2C = 44 bytes)

```cpp
struct __cppobj CUIGuildBBS::CURENTRY : ZRefCounted
{
    int                                     nCurEntryID;     // offset 12
    int                                     nCurCharacterID; // offset 16
    _FILETIME                               ftCurDate;       // offset 20 (8 bytes)
    ZXString<char>                          sCurTitle;       // offset 28
    ZXString<char>                          sCurText;        // offset 32
    int                                     nEmoticon;       // offset 36
    ZArray<ZRef<CUIGuildBBS::COMMENT>>      aComments;       // offset 40
};
```

### `CUIGuildBBS::COMMENT` (`game_types.h:32186`, 0x20 = 32 bytes)

```cpp
struct __cppobj CUIGuildBBS::COMMENT : ZRefCounted
{
    int             m_nSN;          // offset 12
    int             m_nCharacterID; // offset 16
    ZXString<char>  m_sComment;     // offset 20
    _FILETIME       m_ftDate;       // offset 24 (8 bytes)
};
```

### `CWndGuildBoard` (`game_types.h:47691`) — Web Guild Board

```cpp
struct CWndGuildBoard : CWebWnd, TSingleton<CWndGuildBoard>
{
    ZRef<CCtrlButton>   m_pBtModify, m_pBtDelete, m_pBtComment, m_pBtWrite;
    int                 m_bLogined;
    int                 m_nMaskPageType, m_nCodeBoard, m_nOidArticle;
    unsigned int        m_dwArticleOwner;
    unsigned int        m_dwLastAuthUpdateRequest;
    // ...
};
```

`CWndGuildBoard` is not opened by any guild tab button in V95. Its construction is absent from the binary. The `m_nMaskPageType`, `m_nCodeBoard`, `m_nOidArticle` fields are specific to this web view and do not appear in `CUIGuildBBS`.

### BBS UI Limits (inferred from `LoadWriteTextBox`, `game_pseudocode.c:804520`)

No named constants exist. Values are inferred from control-creation parameters:

| Parameter | Value | Source |
|-----------|-------|--------|
| Posts per page | **10** | `OnLoadListResult`: `(totalCount-1)/10+1` page calc; `SetSelectorStart(..., 10, ...)` |
| Posts per guild | **server-enforced** | Not checked client-side |
| Max title length | **25 chars** (display) | `paramEdit.nHorzMax = 25` set for `m_pEditTitle` (`CCtrlEdit`) |
| Body row count | **15 rows** (display) | `paramMLEdit.nRowMax = 15` for `m_pEditText` (`CCtrlMLEdit`) |
| Body line width | **240 px** (display) | `paramMLEdit.nMaxLineWidth = 240` |
| Comments displayed | **4 visible** | `m_pBtCommentDelete[4]` — 4 delete buttons; `m_pSBComment` scrolls further |
| Max comment length | **server-enforced** | No `nHorzMax` set on `m_pEditComment` |

> `DB_GUILDNOTICE_MAX = 0x64` (100) is for the guild **bulletin notice** (`sNotice`), **not** BBS content.

### Notice Pinning — Single-Notice Architecture

The protocol sends exactly **one** optional notice record per `OnLoadListResult` (the `has_notice` prefix byte). The client stores it in a single `ZRef<CUIGuildBBS::ENTRYLIST> m_Notice` slot and renders it in a dedicated region (y=103–134) above the entry list (y=134+).

**Enforcement in `OnWrite` (`game_pseudocode.c:804964`):**

```c
void __thiscall CUIGuildBBS::OnWrite(CUIGuildBBS *this, int bModify, int bNotice)
{
    if (this->m_Notice.p && bNotice) {
        // StringPool 0xEB3: "A notice already exists."
        CUtilDlg::Notice(StringPool::GetString(0xEB3), ...);
        return;  // ← blocks write UI; user must delete old notice first
    }
    // ... open write text box ...
}
```

**Server responsibility:** When the server receives `CP_GuildBBS sub=0, bModify=0, bNotice=1`, it must automatically clear `IsNotice` on the existing notice post before inserting the new one. The client sends **no demote instruction** — it simply refuses to open the UI if a notice exists, trusting that if it got past the guard, there was no existing notice.

**There is no `DB_GUILDBBS_NOTICE_MAX` constant.** The limit of 1 is enforced entirely by the UI guard above.

**Consequence for server implementation:** Allow at most one BBS post per guild with `IsNotice = true`. When `sub=0, bNotice=1` is received, demote the existing notice automatically. Do **not** allow multiple notice posts — the `m_Notice` slot is a single `ZRef`, and the wire format has no room for more than one notice record.

### `CUIGuildBBS` Button IDs (`game_pseudocode.c:806335`)

| Button ID | Decimal | Action | Packet emitted |
|-----------|---------|--------|----------------|
| 0x7D0 | 2000 | Write (new article) | opens write UI — no packet until Register |
| 0x7D1 | 2001 | Write Notice | opens write UI for notice (blocked if notice exists: SP 0xEB3) |
| 0x7D2 | 2002 | Delete | `OnDelete` → sub 1 + `Encode4(nCurEntryID)` |
| 0x7D3 | 2003 | Modify | opens edit UI — no packet until Register |
| 0x7D4 | 2004 | Register (new) | `OnRegister(0,0)` → sub 0, `bModify=0, bNotice=0` |
| 0x7D5 | 2005 | Register (edit) | `OnRegister(1,0)` → sub 0, `bModify=1, bNotice=0` |
| 0x7D6 | 2006 | Register (notice) | `OnRegister(0,1)` → sub 0, `bModify=0, bNotice=1` |
| 0x7D7 | 2007 | Cancel | no packet; calls `SendViewEntryRequest` |
| 0x7D8 | 2008 | Comment | `OnComment` → sub 4 + `Encode4(nCurEntryID)` + `EncodeStr(sComment)` |
| 0x7D9 | 2009 | Delete Comment [0] | `OnCommentDelete(0)` → sub 5 + `Encode4(nCurEntryID)` + `Encode4(m_nSN)` |
| 0x7DA | 2010 | Delete Comment [1] | `OnCommentDelete(1)` → sub 5 |
| 0x7DB | 2011 | Delete Comment [2] | `OnCommentDelete(2)` → sub 5 |
| 0x7DC | 2012 | Delete Comment [3] | `OnCommentDelete(3)` → sub 5 |
| 0x7DD | 2013 | Exit | `UI_Close(UI_GUILDBBS)` — no packet |
| 0x7DE | 2014 | Emoticon ← | `MoveEmoticon(-1)` |
| 0x7DF | 2015 | Emoticon → | `MoveEmoticon(+1)` |

Clicking the notice area or a list entry sets `m_nViewRequestEntryID` and triggers `SendViewEntryRequest` (sub 3 + `Encode4(entryID)`) followed immediately by an automatic `SendLoadListRequest` (sub 2 + `Encode4(nEntryListStart)`).

---

## 14. Guild Boss Field

### CField_GuildBoss

```c
// game_pseudocode.c:275515 — inherits from CField
// Contains CHealer + CPulley sub-objects for guild boss map mechanics
```

### WZ Data

```
Map.wz/{mapId}/
  healer/
    x = 500    (healer NPC X position)
    y = 200    (healer NPC Y position)
  pulley/
    x = 500    (pulley center X)
    y = 200    (pulley center Y)
    hp = 100   (pulley health points)
```

### C++ Structs

```cpp
struct CHealer : ZRefCounted
{
    _com_ptr_t<IWzVector2D> m_pVecOrg;   // movement controller
    _com_ptr_t<IWzGr2DLayer> m_pLayer;   // render layer
};

struct CPulley : ZRefCounted
{
    int m_nHP;
    tagRECT m_rcArea;                     // interaction area
    _com_ptr_t<IWzGr2DLayer> m_pLayer;
};
```

### Packet Handling

```c
// game_pseudocode.c:291268
void CField_GuildBoss::OnPacket(CInPacket *iPacket, int nType)
{
    switch (nType) {
        case 344: // OnHealerMove — Decode2 (y position)
            CHealer::Move(&m_healer, y);
            break;
        case 345: // OnPulleyStateChange — Decode1 (state)
            m_pulley.m_nState = nState;
            break;
        default:
            CField::OnPacket(iPacket, nType);
            break;
    }
}
```

---

## 15. Guild UI (Tab, Sort, Grade Windows)

### UI Tab Indices (CUIUserList)

| Tab | Content | Widget | Buttons |
|-----|---------|--------|---------|
| 0 | Friend | — | 15 |
| 1 | Party | — | 9 |
| 2 | Expedition | — | — |
| 3 | **Guild** | `CTabGuild` | 13 |
| 4 | **Alliance** | `CTabGuildAlliance` | 11 |
| 5 | Blacklist | — | 2 |

### CTabGuild

```cpp
struct CTabGuild
{
    ZRef<CCtrlButton> m_pBtGuild[13];
    IWzCanvas *m_pCanvasMarkDefault;
    CUIUserList *m_pUIUserList;
    ZXString<char> m_sGuildName;
    ZXString<char> m_asGrade[5];
    ZXString<char> m_asGradeOriginal[5];
    ZXString<char> m_sNotice;
    IWzGr2DLayer *m_pLayerNotice;
    int m_nGuildNoticePos;
    CTabGuild::SectionData m_asdOnOff[2];   // [0]=online, [1]=offline
};

struct CTabGuild::GUILDITEM
{
    unsigned int dwMemberID;
    ZXString<char> sMemberName, sJobName;
    int nJob, nLevel, nGrade;
    tagRECT rt;
    ZXString<char> sGradeName;
    tagRECT rcMemberName, rcJobName, rcLevel, rcGrade;
    int bMemberNameReduced, bJobNameReduced;
};

struct CTabGuild::SectionData
{
    int m_bMaximized;
    tagPOINT m_ptBtMaxMin, m_ptBtPage;
    int m_nPageBtSpace, m_nPage, m_nMaxPage;
    tagRECT m_arcArrange[5];
    ZArray<CTabGuild::GUILDITEM> m_aGuildItem;
    CTabGuild::ORDER m_nOrder;
    int m_bAscend;
};
```

### CTabGuildAlliance

Same structure as CTabGuild but with:
- `m_asdOnOff[5]` (one per guild, up to 5 guilds in alliance)
- `GUILDITEM` has additional `nAllianceGrade` field
- 5 members per page per guild section

### Sort Comparators

Both `CTabGuild` and `CTabGuildAlliance` support 4 orderings × 2 directions:

| Comparator | Sort Field | Direction |
|-----------|-----------|-----------|
| `NameAscComp` / `NameDescComp` | sMemberName | A↔Z |
| `JobAscComp` / `JobDescComp` | nJob | Low↔High |
| `LevelAscComp` / `LevelDescComp` | nLevel | Low↔High |
| `GradeAscComp` / `GradeDescComp` | nGrade | Low↔High (Master first) |

Tiebreaker: always `sMemberName` (case-insensitive).

### Sort Order Enum

```cpp
enum CTabGuild::ORDER : __int32
{
    LIST_HEADER = 0x0,
    NAME        = 0x1,
    JOB         = 0x2,
    LEVEL       = 0x3,
    GRADE       = 0x4,
    ORDER_NO    = 0x5,
};
```

### Online/Offline Sections

```cpp
ONLINE   = 0x0,
OFFLINE  = 0x1,
ONOFF_NO = 0x2,
```

### Paging

- Guild tab: **20 members per page** (`ITEMPERPAGE = 0x14`) per online/offline section
- Alliance tab: **5 members per page** per guild section

### CWndGuildGrade / CWndAllianceGrade

```cpp
struct CWndGuildGrade : CWnd, TSingleton<CWndGuildGrade>
{
    ZRef<CCtrlButton> m_pBtGuildGrade[2];
    int m_nSelected;
    CUIUserList *m_pUIUserList;
    ZXString<char> m_asGrade[5], m_asGradeOriginal[5];
    CLayoutMan m_lm;
};
```

- `GetGradeIndexFromPoint`: Maps mouse Y → grade index (1–5)
- Grade rows at y = 75, 95, 115, 135, 155 — each 20px tall, x = 10–254

### CGuildRankDlg

```cpp
struct CGuildRankDlg : CDialog
{
    ZArray<GUILDRANKING> m_aGuildRanking;
    ZRef<CCtrlOriginButton> m_pBtLeft, m_pBtRight, m_pBtOK;
    IWzGr2DLayer *m_pAniLayer;
    int m_nPage;
};
// COUNTPERPAGE = 6 — 6 guilds per page
```

### CCreateGuildAgreeDlg

```cpp
struct CCreateGuildAgreeDlg : CDialog
{
    ZXString<char> m_sMasterName, m_sGuildName;
    ZRef<CCtrlOriginButton> m_pBtYes, m_pBtNo;
    int m_nStatus, m_nAnimationState;
    int m_nAfterAniTimeLeft, m_nShowMsgTimeLeft, m_nWaitChoiceTimeLeft;
};
// Uses same state machine as CSetGuildMarkDlg
```

### Notice Layer

- Guild: scrolling text at (14, 84), 176×15 pixels
- Alliance: scrolling text at (14, 84), 215×15 pixels
- `m_nGuildNoticePos` starts at layer width (scrolls from right)

### Layout Constants

```cpp
kGuild_Name_X1 = 1,   kGuild_Name_X2 = 95,
kGuild_Job_X1 = 97,   kGuild_Job_X2 = 164,
kGuild_Scr_Y = 100,   kGuild_Scr_Len = 185,
kGuild_Basic_H = 105,
kGuild_Mark_Y = 55,   kGuild_Mark_Width = 16,
kGuild_MarkArea_Height = 50,
kGuild_Name_Y = 6,
kGuild_Notice_Y = 84,  kGuild_Notice_W = 176, kGuild_Notice_H = 15,
kGuildAlliance_Notice_W = 215,
kGuildGrade_Width = 264, kGuildGrade_Height = 382,
kGuildGrade_Item_X = 10, kGuildGrade_Item_Y = 148,
kGuildGrade_Item_W = 244, kGuildGrade_Item_H = 20,
kGuildGrade_GP_Y = 77,
kGuildArrange_Name_Width = 62, kGuildArrange_Job_Width = 62,
kGuildArrange_Level_Width = 39, kGuildArrange_Grade_Width = 62,
```

---

## 16. Packet Opcode Summary

### Client → Server

| Header | Sub | Function | Description |
|--------|-----|----------|-------------|
| 149 | 0x00 | (on join self) | Acknowledge guild join |
| 149 | 0x02 | InputGuildName | Create guild with name |
| 149 | 0x05 | SendInviteGuildMsg | Invite player |
| 149 | 0x07 | SendWithdrawGuildMsg | Leave guild |
| 149 | 0x08 | SendKickGuildMsg | Expel member |
| 149 | 0x0D | SendSetGradeNameMsg | Set 5 grade names |
| 149 | 0x0E | SendSetMemberGradeMsg | Change member grade |
| 149 | 0x0F | SendSetGuildMarkMsg | Set guild mark |
| 149 | 0x10 | SendSetGuildNoticeMsg | Set notice |
| 149 | ~~0x1B~~ | ~~GuildReg_SetSkill~~ | **DEAD — never sent by client** (NPC-script-server driven) |
| 149 | 0x20 | SendCreateGuildAgreeMsg | Agree/disagree creation |
| 150 | 55/56 | (invite response) | Auto-decline guild invite |
| 167 | 0x01 | (alliance request) | Request alliance info |
| 168 | 20/21 | (alliance invite resp) | Auto-respond alliance invite |
| 179 | — | GuildBBS | Guild BBS operations |
| ~~289~~ | — | ~~RequestGuildBoardAuthKey~~ | **DEAD — `COutPacket(289)` never constructed; server PUSH only via case 80** |

### Server → Client (CWvsContext::ProcessPacket dispatch)

| Case | Handler | Header |
|------|---------|--------|
| 59 | `OnGuildBBSPacket` | `LP_GuildBBS` (0x3B) |
| 67 | `OnGuildResult` | `LP_GuildResult` (0x43) |
| 68 | `OnAllianceResult` | `LP_AllianceResult` (0x44) |

### User-Level Packets

| Header | Name | Description |
|--------|------|-------------|
| 228 | `LP_UserGuildNameChanged` | Other user's guild name changed |
| 229 | `LP_UserGuildMarkChanged` | Other user's guild mark changed |

---

## 17. StringPool IDs Referenced

| ID (hex) | Message |
|----------|---------|
| 0x15B | "You have invited %s to your guild." |
| 0x15C | "%s denied your guild invitation." |
| 0x15D | "You have joined %s guild." (non-master) |
| 0x15F | "You have been expelled from the guild." (self, voluntary) |
| 0x160 | "%s has been expelled from the guild." (other) |
| 0x161 | "The guild has been disbanded." (self, left) |
| 0x162 | "%s has left the guild." (other, voluntary) |
| 0x164 | "Are you sure you want to leave the guild?" (YesNo) |
| 0x166 | "The guild has been disbanded." (member sees) |
| 0x167 | "You have been invited to a guild." (self join) |
| 0x168 | "%s has joined the guild." (other join) |
| 0x169 | Guild error |
| 0x16A | Guild error |
| 0x16B | "You are not in a guild." |
| 0x16C | "Guild master cannot withdraw." |
| 0x16D | "Cannot send invite." |
| 0x16F | "Only master/jr.master can invite." |
| 0x170 | "Cannot invite yourself." |
| 0x174 | Guild error |
| 0x175 | "The guild is full." |
| 0x176 | Generic guild error |
| 0x177 | Guild error |
| 0xACF | "%s refused your guild invitation." |
| 0xCED | Guild create NPC dialog |
| 0xCEE | Guild error NPC dialog |
| 0xCEF | Guild name in use NPC dialog |
| 0xCF0 | "Guild %s created!" (master) |
| 0xCF1 | Guild disbanded NPC dialog (master) |
| 0xCF2 | "Guild capacity expanded to %d." |
| 0xCF3 | Guild create abort NPC dialog |
| 0xCF4 | Guild error NPC dialog |
| 0xCF5 | Guild disband error NPC dialog |
| 0xCF6 | Guild capacity error NPC dialog |
| 0xD00 | "%s's grade changed to %s." |
| 0xD01 | "Grade names have been updated." |
| 0xD2D | "Guild mark has been changed." |
| 0xD36 | "Guild notice: %s" |
| 0xDFD | Guild skill error |
| 0xDFE | Guild skill error |
| 0xDFF | "Guild skill activated in channel %s." |
| 0xE00 | "Guild skill already active in channel %s." |
| 0xE01 | "Guild skill: %d remaining." |
| 0xEB1 | "Please enter content." (empty title or body guard in `OnRegister`/`OnComment`) |
| 0xEB2 | "Are you sure you want to delete?" (YesNo confirm in `OnDelete` / `OnCommentDelete`) |
| 0xEB3 | "A notice already exists." (shown by `OnWrite` when `m_Notice.p != 0 && bNotice`) |
| 0xEC7 | Entry not found (shown by `CUIGuildBBS::OnEntryNotFound`) |
| 0x1A15 | Page number format `"Page %d"` (used by `OnLoadListResult` to populate `CCtrlSelector` labels) |
| 0x1881 | "Joined alliance %s." |
| 0x1882 | "Guild %s has joined the alliance." |
| 0x1897 | "Left the alliance." |
| 0x1898 | "Expelled from the alliance." |
| 0x18A0 | "Guild board access revoked." |
| 0x18A3 | "No alliance members online." |
| 0x1A8C | "You have been expelled." (case 49, self) |
