using System.Collections.Immutable;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Services.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Broadcasts updated grade name strings to all online guild members.
/// Wire: LP_GuildResult GuildRes_SetGradeName_Done (0x40).
/// Payload: guildID(4) + grade1(str) + grade2(str) + grade3(str) + grade4(str) + grade5(str).
/// </summary>
public class NotifyGuildGradeNamesChangedPlug : IPipelinePlug<NotifyGuildGradeNamesChanged>
{
    private readonly IGameStage _stage;

    public NotifyGuildGradeNamesChangedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildGradeNamesChanged message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users.Where(u => u.Guild?.ID == message.GuildID).ToImmutableArray();

        foreach (var user in affected)
        {
            // Keep grade name strings in the local snapshot current.
            if (user.Guild is GuildMembership ms && message.GradeNames.Length >= 5)
            {
                ms.GradeName1 = message.GradeNames[0];
                ms.GradeName2 = message.GradeNames[1];
                ms.GradeName3 = message.GradeNames[2];
                ms.GradeName4 = message.GradeNames[3];
                ms.GradeName5 = message.GradeNames[4];
            }

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.SetGradeName_Done);
            packet.WriteInt(message.GuildID);
            foreach (var name in message.GradeNames)
                packet.WriteString(name);
            _ = user.Dispatch(packet.Build());
        }
    }
}
