using Edelstein.Common.Crypto;
using Edelstein.Common.Gameplay.Game.Combat.Damage;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Combat.Damage;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Utilities.Packets;
using Edelstein.Protocol.Utilities.Pipelines;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

public abstract class AbstractUserAttackHandler : AbstractPipedFieldHandler<FieldOnPacketUserAttack>
{
    protected abstract AttackType Type { get; }

    private readonly ILogger _logger;

    protected AbstractUserAttackHandler(IPipeline<FieldOnPacketUserAttack> pipeline, ILogger logger)
        : base(pipeline)
    {
        _logger = logger;
    }

    protected override FieldOnPacketUserAttack? Serialize(IFieldUser user, IPacketReader reader)
    {
        var attack = reader.Read(new Attack(Type));

        if (!CrcValidator.ValidateAttackDrCrc(attack.Dr2, attack.Dr3, attack.AttackCrc))
        {
            _logger.LogWarning(
                "Attack CRC mismatch for character {CharacterID}: dr2={Dr2} dr3={Dr3} crc={Crc}",
                user.Character.ID,
                attack.Dr2,
                attack.Dr3,
                attack.AttackCrc
            );
            _ = user.StageUser.Disconnect();
            return null;
        }

        foreach (var mob in attack.MobEntries)
        {
            if (!CrcValidator.ValidateMobCrc(mob.MobID, mob.MobCrc))
            {
                _logger.LogWarning(
                    "Mob CRC mismatch on attack packet for character {CharacterID}: mobObjId={MobID} crc={Crc}",
                    user.Character.ID,
                    mob.MobID,
                    mob.MobCrc
                );
                _ = user.StageUser.Disconnect();
                return null;
            }
        }

        return new(user, attack);
    }
}
