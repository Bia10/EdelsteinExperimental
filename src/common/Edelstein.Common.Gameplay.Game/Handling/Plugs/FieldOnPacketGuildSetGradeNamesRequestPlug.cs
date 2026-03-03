using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Updates all five guild grade name strings.
/// On success <c>NotifyGuildGradeNamesChanged</c> broadcasts
/// <c>SetGradeName_Done (0x40)</c> to all online guild members.
/// </summary>
public class FieldOnPacketGuildSetGradeNamesRequestPlug : IPipelinePlug<FieldOnPacketGuildSetGradeNamesRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildSetGradeNamesRequest message)
    {
        if (message.User.StageUser.Guild == null || message.GradeNames.Length < 5)
            return;

        await message.User.StageUser.Context.Services.Guild.SetGradeNames(
            new GuildSetGradeNamesRequest(
                message.User.StageUser.Guild.ID,
                message.User.Character.ID,
                message.GradeNames[0],
                message.GradeNames[1],
                message.GradeNames[2],
                message.GradeNames[3],
                message.GradeNames[4]
            ));
    }
}
