using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles the <c>NotifyGuildCreated</c> message-bus event.
/// Finds the creating character, sets <c>user.Guild</c>, and sends the
/// <c>CreateNewGuild_Done (0x22)</c> packet with the full <c>GUILDDATA</c>.
/// </summary>
public class NotifyGuildCreatedPlug : IPipelinePlug<NotifyGuildCreated>
{
    private readonly IGameStage _stage;

    public NotifyGuildCreatedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildCreated message)
    {
        var user = await _stage.Users.Retrieve(message.CharacterID);
        if (user == null) return;

        user.Guild = message.Guild;

        using var packet = new PacketWriter(PacketSendOperations.GuildResult);
        packet.WriteByte((byte)GuildResultOperations.CreateNewGuild_Done);
        packet.WriteByte(0); // hasData = 0 → full GUILDDATA decode follows (ref §11 case 28)
        packet.WriteGuildData(message.Guild);
        _ = user.Dispatch(packet.Build());
    }
}
