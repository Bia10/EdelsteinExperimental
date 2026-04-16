namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>Generic guild operation response carrying a <see cref="GuildResult"/> code.</summary>
public record GuildResponse(GuildResult Result = GuildResult.Success);
