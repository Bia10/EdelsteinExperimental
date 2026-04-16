using Edelstein.Protocol.Services.Social;

namespace Edelstein.Protocol.Gameplay.Contracts;

public record NotifyGuildMemberJoined(
    int GuildID,
    IGuildMembership GuildMembership,
    IGuildMember NewMember
);
