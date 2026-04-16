using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Changes a member's guild grade tier (master-only operation).
/// On success <c>NotifyGuildMemberGradeChanged</c> broadcasts
/// <c>SetMemberGrade_Done (0x42)</c> to all online guild members.
/// </summary>
public class FieldOnPacketGuildSetMemberGradeRequestPlug
    : IPipelinePlug<FieldOnPacketGuildSetMemberGradeRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildSetMemberGradeRequest message)
    {
        if (message.User.StageUser.Guild == null)
            return;

        await message.User.StageUser.Context.Services.Guild.SetMemberGrade(
            new GuildSetMemberGradeRequest(
                message.User.StageUser.Guild.ID,
                message.User.Character.ID, // masterID
                message.TargetCharacterID,
                message.Grade
            )
        );
    }
}
