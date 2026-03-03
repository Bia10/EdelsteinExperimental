namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Categorised result codes returned by all guild service operations.</summary>
public enum GuildResult
{
    Success,
    FailedUnknown,
    FailedAlreadyInGuild,
    FailedNotInGuild,
    FailedNotMaster,
    FailedNameTaken,
    FailedNameInvalid,
    FailedFull,
    FailedAlreadyInvited,
    FailedNotInvited,
    FailedSelf,
    FailedCharacterNotFound,
    FailedBeginner,
    FailedNotEnoughGP,
    FailedGuildNotFound,
}
