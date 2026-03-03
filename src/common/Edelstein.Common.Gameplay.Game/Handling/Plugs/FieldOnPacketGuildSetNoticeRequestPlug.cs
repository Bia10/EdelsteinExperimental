using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Updates the guild notice text.
/// On success <c>NotifyGuildNoticeChanged</c> broadcasts <c>SetNotice_Done (0x47)</c>
/// to all online guild members.
/// </summary>
public class FieldOnPacketGuildSetNoticeRequestPlug : IPipelinePlug<FieldOnPacketGuildSetNoticeRequest>
{
    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildSetNoticeRequest message)
    {
        if (message.User.StageUser.Guild == null)
            return;

        await message.User.StageUser.Context.Services.Guild.SetNotice(
            new GuildSetNoticeRequest(
                message.User.StageUser.Guild.ID,
                message.User.Character.ID,
                message.Notice
            ));
    }
}
