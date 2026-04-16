using Edelstein.Protocol.Services.Social;

namespace Edelstein.Protocol.Gameplay.Contracts;

public record NotifyGuildCreated(int CharacterID, IGuildMembership Guild);
