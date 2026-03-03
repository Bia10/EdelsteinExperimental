namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>
/// Accepts a pending guild invitation identified by <paramref name="InviterID"/> and
/// <paramref name="CharacterID"/>. The server resolves the guildID from the stored
/// invitation record rather than trusting the client-supplied inviterID.
/// </summary>
public record GuildInviteAcceptRequest(
    int InviterID,
    int CharacterID,
    string CharacterName,
    int Job,
    int Level,
    int ChannelID,
    int FieldID
);
