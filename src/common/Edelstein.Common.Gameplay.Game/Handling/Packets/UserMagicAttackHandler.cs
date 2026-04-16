using Edelstein.Common.Gameplay.Handling;
using Edelstein.Protocol.Gameplay.Game.Combat.Damage;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

public class UserMagicAttackHandler : AbstractUserAttackHandler
{
    public override short Operation => (short)PacketRecvOperations.UserMagicAttack;
    protected override AttackType Type => AttackType.Magic;

    public UserMagicAttackHandler(
        IPipeline<FieldOnPacketUserAttack> pipeline,
        ILogger<UserMagicAttackHandler> logger
    )
        : base(pipeline, logger) { }
}
