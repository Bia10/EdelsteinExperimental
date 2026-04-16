using Edelstein.Common.Crypto;
using Edelstein.Common.Gameplay.Game.Objects.Mob;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Gameplay.Game.Objects.Mob;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Utilities.Packets;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

public class MobMoveHandler : AbstractPipedFieldMobHandler<FieldOnPacketMobMove>
{
    public MobMoveHandler(IPipeline<FieldOnPacketMobMove> pipeline)
        : base(pipeline) { }

    public override short Operation => (short)PacketRecvOperations.MobMove;

    protected override FieldOnPacketMobMove? Serialize(
        IFieldUser user,
        IFieldMob mob,
        IPacketReader reader
    )
    {
        var movePath = reader.Read(new FieldMobMovePath());
        var crcKey = user.StageUser.CrcKey;
        var expectedCrc = CrcCalculator.Compute(movePath.HackedCode, crcKey);

        if (expectedCrc != movePath.HackedCodeCrc)
        {
            using var failPacket = new PacketWriter(PacketSendOperations.DataCRCCheckFailed);
            _ = user.Dispatch(failPacket.Build());
            return null;
        }

        return new(user, mob, movePath);
    }
}
