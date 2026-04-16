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
/// Handles the <c>NotifyGuildMemberJoined</c> message-bus event.
/// Sends <c>JoinGuild_Done (0x29)</c> to every online guild member including
/// the newly joined character.
/// <para>
/// Wire layout (V95 §11 case 41):
/// guildID(4) + newCharID(4) + GUILDMEMBER(37 bytes).
/// </para>
/// <para>
/// The new member's client auto-responds with CP_GuildRequest sub 0x00,
/// triggering a full <c>LoadGuild_Done (0x1C)</c> GUILDDATA delivery handled
/// by <see cref="Packets.GuildRequestHandler"/>.
/// Existing members' clients insert the new member into their local roster.
/// </para>
/// </summary>
public class NotifyGuildMemberJoinedPlug : IPipelinePlug<NotifyGuildMemberJoined>
{
    private readonly IGameStage _stage;

    public NotifyGuildMemberJoinedPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildMemberJoined message)
    {
        var users = await _stage.Users.RetrieveAll();
        var guildMembers = users
            .Where(u => u.Guild?.ID == message.GuildID)
            .Append(
                await _stage.Users.Retrieve(message.NewMember.CharacterID) is { } newUser
                    ? newUser
                    : null!
            )
            .Where(u => u != null)
            .DistinctBy(u => u.Character?.ID ?? 0)
            .ToImmutableArray();

        foreach (var user in guildMembers)
        {
            var isNewMember = user.Character?.ID == message.NewMember.CharacterID;

            if (!isNewMember && user.Guild != null)
            {
                // Eagerly update the existing member's in-memory roster snapshot.
                user.Guild.Members[message.NewMember.CharacterID] = new GuildMembershipMember(
                    message.NewMember
                );
            }

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)GuildResultOperations.JoinGuild_Done);
            packet.WriteInt(message.GuildID);
            packet.WriteInt(message.NewMember.CharacterID);

            if (!isNewMember)
            {
                // R-002 case 41: existing members receive the full GUILDMEMBER
                // struct (37 bytes) so they can insert the newcomer into their
                // local roster immediately without a separate load.
                packet.WriteGuildMember(message.NewMember);
            }
            // The new member receives guildID + charID only. Their client
            // auto-responds with CP_GuildRequest sub 0x00 (LoadGuild), which
            // triggers HandleLoadGuildAsync to deliver the full GUILDDATA.

            _ = user.Dispatch(packet.Build());
        }
    }
}
