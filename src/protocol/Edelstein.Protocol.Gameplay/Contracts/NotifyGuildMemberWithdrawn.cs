namespace Edelstein.Protocol.Gameplay.Contracts;

public record NotifyGuildMemberWithdrawn(
    int GuildID,
    int CharacterID,
    string CharacterName,
    bool IsKicked
);
