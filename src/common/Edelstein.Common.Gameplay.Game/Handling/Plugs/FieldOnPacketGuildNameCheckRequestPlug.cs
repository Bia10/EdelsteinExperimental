using System.Collections.Immutable;
using System.Linq;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles a guild name check request (CP_GuildRequest sub 0x02).
///
/// V95 creation flow (R-003):
/// (1) Client sends 0x02 with proposed guild name.
/// (2) Server validates the name.
/// (3) Server sends case 3 (<see cref="GuildResultOperations.CreateGuildAgree"/>)
///     to every online party member on this stage:
///     - Boss gets the opcode only (no data — NPC dialog driven).
///     - Non-boss members get: partyID(4) + requesterName(str) + guildName(str).
/// (4) Server immediately creates the guild; GuildReq_CreateNewGuild (0x04) is
///     a dead client opcode in V95 (R-003). The party member 0x20 agree-replies
///     are informational and require no further server action.
/// (5) On success <c>NotifyGuildCreated</c> delivers case 34 (full GUILDDATA)
///     to the creator via the message bus.
/// </summary>
public class FieldOnPacketGuildNameCheckRequestPlug : IPipelinePlug<FieldOnPacketGuildNameCheckRequest>
{
    private readonly IGameStage _stage;

    public FieldOnPacketGuildNameCheckRequestPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildNameCheckRequest message)
    {
        var response = await message.User.StageUser.Context.Services.Guild.CheckName(
            new GuildNameCheckRequest(message.GuildName));

        if (response.Result != GuildResult.Success)
        {
            var errOpcode = response.Result == GuildResult.FailedNameTaken
                ? GuildResultOperations.CheckGuildName_AlreadyUsed
                : GuildResultOperations.CheckGuildName_Unknown;
            using var err = new PacketWriter(PacketSendOperations.GuildResult);
            err.WriteByte((byte)errOpcode);
            await message.User.Dispatch(err.Build());
            return;
        }

        // Name is valid. Broadcast the agree dialog to all online party members.
        var party = message.User.StageUser.Party;
        if (party != null)
        {
            var allUsers = await _stage.Users.RetrieveAll();
            var partyUsers = allUsers
                .Where(u => party.Members.ContainsKey(u.Character?.ID ?? 0))
                .ToImmutableList();

            foreach (var partyUser in partyUsers)
            {
                var isBoss = partyUser.Character?.ID == party.BossCharacterID;
                using var agreePacket = new PacketWriter(PacketSendOperations.GuildResult);
                agreePacket.WriteByte((byte)GuildResultOperations.CreateGuildAgree);
                if (!isBoss)
                {
                    // Non-boss: attach context so the agree dialog can display names.
                    agreePacket.WriteInt(party.ID);
                    agreePacket.WriteString(message.User.Character.Name);
                    agreePacket.WriteString(message.GuildName);
                }
                _ = partyUser.Dispatch(agreePacket.Build());
            }
        }

        // Create the guild. NotifyGuildCreated will send case 34 to the creator.
        await message.User.StageUser.Context.Services.Guild.Create(
            new GuildCreateRequest(
                message.User.Character.ID,
                message.User.Character.Name,
                message.User.Character.Job,
                message.User.Character.Level,
                message.User.StageUser.Context.Options.ChannelID,
                message.User.Field?.ID ?? 999999999,
                message.GuildName
            ));
    }
}
