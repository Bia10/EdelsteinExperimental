namespace Edelstein.Common.Gameplay.Social;

/// <summary>
/// Sub-operation codes written as the first byte of outbound
/// <c>CP_GuildRequest (0x95)</c> packets sent from client to server.
/// Values taken verbatim from the V95 client enum.
/// </summary>
public enum GuildRequestOperations : byte
{
    LoadGuild = 0x00,
    InputGuildName = 0x01,
    CheckGuildName = 0x02,
    CreateGuildAgree = 0x03,
    CreateNewGuild = 0x04,
    InviteGuild = 0x05,
    JoinGuild = 0x06,
    WithdrawGuild = 0x07,
    KickGuild = 0x08,
    RemoveGuild = 0x09,
    IncMaxMemberNum = 0x0A,
    ChangeLevel = 0x0B,
    ChangeJob = 0x0C,
    SetGradeName = 0x0D,
    SetMemberGrade = 0x0E,
    SetMark = 0x0F,
    SetNotice = 0x10,
    InputMark = 0x11,
    CheckQuestWaiting = 0x12,
    CheckQuestWaiting2 = 0x13,
    InsertQuestWaiting = 0x14,
    CancelQuestWaiting = 0x15,
    RemoveQuestCompleteGuild = 0x16,
    IncPoint = 0x17,
    IncCommitment = 0x18,
    SetQuestTime = 0x19,
    ShowGuildRanking = 0x1A,
    SetSkill = 0x1B,
}
