using Edelstein.Common.Crypto;
using Edelstein.Common.Gameplay.Game.Objects.Mob;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Gameplay.Game.Objects.Mob;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Utilities.Packets;
using Edelstein.Protocol.Utilities.Pipelines;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

public class MobMoveHandler : AbstractPipedFieldMobHandler<FieldOnPacketMobMove>
{
    private readonly ILogger<MobMoveHandler> _logger;

    public MobMoveHandler(IPipeline<FieldOnPacketMobMove> pipeline, ILogger<MobMoveHandler> logger)
        : base(pipeline)
    {
        _logger = logger;
    }

    public override short Operation => (short)PacketRecvOperations.MobMove;

    protected override FieldOnPacketMobMove? Serialize(
        IFieldUser user,
        IFieldMob mob,
        IPacketReader reader
    )
    {
        var movePath = reader.Read(new FieldMobMovePath());

        if (movePath.CheatedRandom || movePath.CheatedCtrlMove)
        {
            _logger.LogWarning(
                "Cheat flags set in MobMove for character {CharacterID}: CheatedRandom={CheatedRandom} CheatedCtrlMove={CheatedCtrlMove}",
                user.Character.ID,
                movePath.CheatedRandom,
                movePath.CheatedCtrlMove
            );
            _ = user.StageUser.Disconnect();
            return null;
        }

        if (!CrcValidator.ValidateHackedCode(movePath.HackedCode, movePath.HackedCodeCrc))
        {
            _logger.LogWarning(
                "HackedCode CRC mismatch in MobMove for character {CharacterID}: hackedCode={HackedCode} crc={HackedCodeCrc}",
                user.Character.ID,
                movePath.HackedCode,
                movePath.HackedCodeCrc
            );
            _ = user.StageUser.Disconnect();
            return null;
        }

        return new(user, mob, movePath);
    }
}
