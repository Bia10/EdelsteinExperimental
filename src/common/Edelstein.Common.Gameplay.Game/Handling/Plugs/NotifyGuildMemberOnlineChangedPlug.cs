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
/// Broadcasts a member's online or offline transition to all guild members on this stage.
/// Wire: LP_GuildResult GuildRes_NotifyLoginOrLogout (0x3F).
/// Payload: guildID(4) + charID(4) + bOnLine(1).
/// </summary>
public class NotifyGuildMemberOnlineChangedPlug : IPipelinePlug<NotifyGuildMemberOnlineChanged>
{
    private readonly IGameStage _stage;

    public NotifyGuildMemberOnlineChangedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildMemberOnlineChanged message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users.Where(u => u.Guild?.ID == message.GuildID).ToImmutableArray();

        // Offline sentinel used throughout the codebase is −2.
        var updatedChannelID = message.IsOnline ? 0 : -2;

        foreach (var user in affected)
        {
            // Mirror the online status change into the local snapshot.
            if (
                user.Guild?.Members.TryGetValue(message.CharacterID, out var member) == true
                && member is GuildMembershipMember m
            )
            {
                m.ChannelID = updatedChannelID;
            }

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.NotifyLoginOrLogout);
            packet.WriteInt(message.GuildID);
            packet.WriteInt(message.CharacterID);
            packet.WriteBool(message.IsOnline);
            _ = user.Dispatch(packet.Build());
        }
    }
}
