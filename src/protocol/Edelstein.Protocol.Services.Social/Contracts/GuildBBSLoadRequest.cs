namespace Edelstein.Protocol.Services.Social.Contracts;

/// <summary>
/// Requests the guild BBS post list for the given guild.
/// <para>
/// <paramref name="EntryListStart"/> is the 0-based entry offset sent by the client
/// as <c>nEntryListStart</c> (always a multiple of 10, e.g. 0, 10, 20 …).
/// </para>
/// </summary>
public record GuildBBSLoadRequest(int GuildID, int EntryListStart = 0);
