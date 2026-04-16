using Edelstein.Protocol.Services.Social;

namespace Edelstein.Protocol.Gameplay.Contracts;

/// <summary>
/// Broadcast when any guild header data changes: notice, grade names, emblem,
/// member grade, level/job update, or member channel change.
/// </summary>
public record NotifyGuildUpdated(int GuildID, IGuildMembership GuildMembership);
