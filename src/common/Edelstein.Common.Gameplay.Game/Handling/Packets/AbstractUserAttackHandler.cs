using Edelstein.Common.Crypto;
using Edelstein.Common.Gameplay.Game.Combat.Damage;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Combat.Damage;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Utilities.Packets;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

public abstract class AbstractUserAttackHandler : AbstractPipedFieldHandler<FieldOnPacketUserAttack>
{
    protected abstract AttackType Type { get; }

    protected AbstractUserAttackHandler(IPipeline<FieldOnPacketUserAttack> pipeline)
        : base(pipeline) { }

    protected override FieldOnPacketUserAttack? Serialize(IFieldUser user, IPacketReader reader)
    {
        var attack = reader.Read(new Attack(Type));
        var crcKey = user.StageUser.CrcKey;
        var expectedCrc = CrcCalculator.Compute(attack.DrRand, crcKey);

        if (expectedCrc != attack.Crc)
        {
            using var failPacket = new PacketWriter(PacketSendOperations.DataCRCCheckFailed);
            _ = user.Dispatch(failPacket.Build());
            return null;
        }

        return new(user, attack);
    }
}
