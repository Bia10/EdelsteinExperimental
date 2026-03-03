using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Updates the guild emblem (mark).
/// On success <c>NotifyGuildMarkChanged</c> broadcasts
/// <c>SetMark_Done (0x45)</c> to all online guild members.
/// </summary>
public class FieldOnPacketGuildSetMarkRequestPlug : IPipelinePlug<FieldOnPacketGuildSetMarkRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildSetMarkRequest message)
    {
        if (message.User.StageUser.Guild == null)
            return;

        await message.User.StageUser.Context.Services.Guild.SetMark(
            new GuildSetMarkRequest(
                message.User.StageUser.Guild.ID,
                message.User.Character.ID,
                message.MarkBg,
                message.MarkBgColor,
                message.Mark,
                message.MarkColor
            ));
    }
}
