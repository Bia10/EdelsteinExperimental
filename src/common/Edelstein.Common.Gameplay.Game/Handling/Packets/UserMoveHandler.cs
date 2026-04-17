using Edelstein.Common.Gameplay.Game.Objects.User;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Utilities.Packets;
using Edelstein.Protocol.Utilities.Pipelines;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

public class UserMoveHandler : AbstractPipedFieldHandler<FieldOnPacketUserMove>
{
    private readonly ILogger<UserMoveHandler> _logger;

    public UserMoveHandler(
        IPipeline<FieldOnPacketUserMove> pipeline,
        ILogger<UserMoveHandler> logger
    )
        : base(pipeline) => _logger = logger;

    public override short Operation => (short)PacketRecvOperations.UserMove;

    protected override FieldOnPacketUserMove? Serialize(IFieldUser user, IPacketReader reader)
    {
        // 29-byte movement header (CMovePath::Flush encoding, V95 layout — provisional):
        // [0-3]   nPortalCount   int32
        // [4-7]   nDrEff0        int32  (anti-cheat random factor 0)
        // [8-11]  nDrEff1        int32  (anti-cheat random factor 1)
        // [12-15] nDrEff2        int32  (anti-cheat random factor 2)
        // [16-19] nDrEff3        int32  (anti-cheat random factor 3)
        // [20-23] m_dwCrc        int32  (CField::m_dwCrc — field composite CRC)
        // [24-27] nFieldKey      int32  (anti-cheat field key)
        // [28]    bLeft          byte   (character facing direction)
        //
        // NOTE: The exact offset of m_dwCrc within these 29 bytes is derived from
        // known V95 CMovePath::Flush analysis. Confirm against a live packet capture
        // before enabling strict disconnect-on-mismatch.
        _ = reader.ReadInt(); // nPortalCount
        _ = reader.ReadInt(); // nDrEff0
        _ = reader.ReadInt(); // nDrEff1
        _ = reader.ReadInt(); // nDrEff2
        _ = reader.ReadInt(); // nDrEff3
        var dwCrc = (uint)reader.ReadInt(); // m_dwCrc — field composite CRC
        _ = reader.ReadInt(); // nFieldKey
        _ = reader.ReadByte(); // bLeft

        var expectedCrc = user.Field?.Template.CrcValue ?? 0u;
        if (dwCrc != expectedCrc)
            _logger.LogDebug(
                "Field CRC mismatch for user {CharacterId} in field {FieldId}: client=0x{ClientCrc:X8} expected=0x{ExpectedCrc:X8}",
                user.Character.ID,
                user.Field?.ID,
                dwCrc,
                expectedCrc
            );

        return new(user, reader.Read(new FieldUserMovePath()));
    }
}
