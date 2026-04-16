namespace Edelstein.Protocol.Services.Social.Contracts;

public record GuildLoadResponse(
    GuildResult Result = GuildResult.Success,
    IGuildMembership? GuildMembership = null
);
