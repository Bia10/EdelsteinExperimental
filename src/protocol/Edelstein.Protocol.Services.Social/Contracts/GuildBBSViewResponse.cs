namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Returns the full post plus its comment list, or a not-found result.</summary>
public record GuildBBSViewResponse(
    GuildResult Result = GuildResult.Success,
    IGuildBBSPost? Post = null,
    IReadOnlyList<IGuildBBSComment>? Comments = null
);
