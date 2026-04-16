using Edelstein.Common.Gameplay.Handling;
using Edelstein.Protocol.Gameplay.Game.Combat.Damage;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

public class UserShootAttackHandler : AbstractUserAttackHandler
{
    public override short Operation => (short)PacketRecvOperations.UserShootAttack;
    protected override AttackType Type => AttackType.Shoot;

    public UserShootAttackHandler(
        IPipeline<FieldOnPacketUserAttack> pipeline,
        ILogger<UserShootAttackHandler> logger
    )
        : base(pipeline, logger) { }
}
