namespace Edelstein.Protocol.Gameplay.Contracts;

public record NotifyGuildMemberInvited(
    int InviterID,
    string InviterName,
    int GuildID,
    string GuildName,
    int TargetCharacterID
);
