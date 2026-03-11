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
/// Handles the <c>NotifyGuildUpdated</c> message-bus event.
/// Sends a full <c>LoadGuild_Done (0x1C)</c> GUILDDATA refresh to all online
/// guild members. Used when guild-wide state (point, level, skill records, etc.)
/// changes in a way that requires a complete client-side resync.
/// The GUILDDATA payload is identical for every member; the client locates its own
/// character in the <c>adwCharacterID</c> array to determine personal membership state.
/// </summary>
public class NotifyGuildUpdatedPlug : IPipelinePlug<NotifyGuildUpdated>
{
    private readonly IGameStage _stage;

    public NotifyGuildUpdatedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildUpdated message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users
            .Where(u => u.Guild?.ID == message.GuildID)
            .ToImmutableArray();

        foreach (var user in affected)
        {
            // Refresh shared guild-header fields in the local snapshot while
            // preserving each member's personal fields (CharacterID, Grade, etc.).
            if (user.Guild is GuildMembership ms)
            {
                ms.Name              = message.GuildMembership.Name;
                ms.GradeName1        = message.GuildMembership.GradeName1;
                ms.GradeName2        = message.GuildMembership.GradeName2;
                ms.GradeName3        = message.GuildMembership.GradeName3;
                ms.GradeName4        = message.GuildMembership.GradeName4;
                ms.GradeName5        = message.GuildMembership.GradeName5;
                ms.MaxMemberNum      = message.GuildMembership.MaxMemberNum;
                ms.MasterCharacterID = message.GuildMembership.MasterCharacterID;
                ms.MarkBg            = message.GuildMembership.MarkBg;
                ms.MarkBgColor       = message.GuildMembership.MarkBgColor;
                ms.Mark              = message.GuildMembership.Mark;
                ms.MarkColor         = message.GuildMembership.MarkColor;
                ms.Notice            = message.GuildMembership.Notice;
                ms.Point             = message.GuildMembership.Point;
                ms.GuildLevel        = message.GuildMembership.GuildLevel;
                ms.AllianceID        = message.GuildMembership.AllianceID;
                ms.Members           = message.GuildMembership.Members;
                ms.Skills            = message.GuildMembership.Skills;
            }

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.LoadGuild_Done);
            packet.WriteByte(0); // hasData = 0 → full GUILDDATA decode follows (ref §11 case 28)
            packet.WriteGuildData(message.GuildMembership);
            _ = user.Dispatch(packet.Build());
        }
    }
}
