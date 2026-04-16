namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>
/// Asks the guild service whether a proposed name is available for creation.
/// </summary>
public record GuildNameCheckRequest(string GuildName);
