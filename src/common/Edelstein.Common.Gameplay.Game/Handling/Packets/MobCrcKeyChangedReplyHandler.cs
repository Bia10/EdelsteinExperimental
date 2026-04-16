using Edelstein.Common.Gameplay.Handling;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Utilities.Packets;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

public class MobCrcKeyChangedReplyHandler : IPacketHandler<IGameStageUser>
{
    public short Operation => (short)PacketRecvOperations.MobCrcKeyChangedReply;

    public bool Check(IGameStageUser user) => true;

    public Task Handle(IGameStageUser user, IPacketReader reader)
    {
        _ = reader.ReadInt(); // objectID echoed from server
        _ = reader.ReadInt(); // client reply value
        return Task.CompletedTask;
    }
}
