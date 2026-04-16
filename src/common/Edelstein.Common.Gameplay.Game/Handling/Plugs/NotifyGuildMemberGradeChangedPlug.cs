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
/// Broadcasts a member grade tier change to all online guild members.
/// Wire: LP_GuildResult GuildRes_SetMemberGrade_Done (0x42).
/// Payload: guildID(4) + charID(4) + grade(1).
/// </summary>
public class NotifyGuildMemberGradeChangedPlug : IPipelinePlug<NotifyGuildMemberGradeChanged>
{
    private readonly IGameStage _stage;

    public NotifyGuildMemberGradeChangedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildMemberGradeChanged message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users.Where(u => u.Guild?.ID == message.GuildID).ToImmutableArray();

        foreach (var user in affected)
        {
            // Update the affected member's grade in the local snapshot.
            if (
                user.Guild?.Members.TryGetValue(message.CharacterID, out var member) == true
                && member is GuildMembershipMember m
            )
            {
                m.Grade = message.Grade;
            }

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.SetMemberGrade_Done);
            packet.WriteInt(message.GuildID);
            packet.WriteInt(message.CharacterID);
            packet.WriteByte((byte)message.Grade);
            _ = user.Dispatch(packet.Build());
        }
    }
}
