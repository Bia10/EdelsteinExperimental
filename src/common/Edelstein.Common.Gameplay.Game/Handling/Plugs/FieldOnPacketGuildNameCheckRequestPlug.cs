using System.Collections.Immutable;
using System.Linq;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Services.Social;
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
/// (2) Server validates the name, party size, and creation fee.
/// (3) Server sends case 3 (<see cref="GuildResultOperations.CreateGuildAgree"/>)
///     to every online party member on this stage:
///     - Boss gets the opcode only (no data — NPC dialog driven).
///     - Non-boss members get: partyID(4) + requesterName(str) + guildName(str).
/// (4) Server immediately creates the guild; GuildReq_CreateNewGuild (0x04) is
///     a dead client opcode in V95 (R-003). The party member 0x20 agree-replies
///     are informational and require no further server action.
/// (5) On success the creation fee is deducted and <c>NotifyGuildCreated</c>
///     delivers case 34 (full GUILDDATA) to the creator via the message bus.
/// </summary>
public class FieldOnPacketGuildNameCheckRequestPlug
    : IPipelinePlug<FieldOnPacketGuildNameCheckRequest>
{
    private readonly IGameStage _stage;
    private readonly GuildOptions _options;

    public FieldOnPacketGuildNameCheckRequestPlug(IGameStage stage, GuildOptions options)
    {
        _stage = stage;
        _options = options;
    }

    public async Task Handle(IPipelineContext ctx, FieldOnPacketGuildNameCheckRequest message)
    {
        // Step 1 — validate the proposed guild name via the service.
        var nameCheck = await message.User.StageUser.Context.Services.Guild.CheckName(
            new GuildNameCheckRequest(message.GuildName)
        );

        if (nameCheck.Result != GuildResult.Success)
        {
            var errOpcode =
                nameCheck.Result == GuildResult.FailedNameTaken
                    ? GuildResultOperations.CheckGuildName_AlreadyUsed
                    : GuildResultOperations.CheckGuildName_Unknown;
            using var err = new PacketWriter(PacketSendOperations.GuildResult);
            err.WriteByte((byte)errOpcode);
            await message.User.Dispatch(err.Build());
            return;
        }

        // TODO: later propably move to a plugin as configurable override, for v95 6 members of pt are required
        // however later that requirement is reduced to 1 anyways
        // Step 2 — enforce the required party size (skip when RequiredPartySize <= 1
        // so that solo creation works when the config is set to 1).
        if (_options.RequiredPartySize > 1)
        {
            var partySize = message.User.StageUser.Party?.Members.Count ?? 0;
            if (partySize < _options.RequiredPartySize)
            {
                using var err = new PacketWriter(PacketSendOperations.GuildResult);
                err.WriteByte((byte)GuildResultOperations.CreateNewGuild_NotFullParty);
                await message.User.Dispatch(err.Build());
                return;
            }
        }

        // Step 3 — check the player has enough meso to cover the creation fee.
        if (_options.CreationFee > 0 && message.User.Character.Money < _options.CreationFee)
        {
            using var err = new PacketWriter(PacketSendOperations.GuildResult);
            err.WriteByte((byte)GuildResultOperations.CreateNewGuild_Unknown);
            await message.User.Dispatch(err.Build());
            return;
        }

        // Step 4 — broadcast the agree dialog to all online party members.
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

        // Step 5 — create the guild.
        var createResponse = await message.User.StageUser.Context.Services.Guild.Create(
            new GuildCreateRequest(
                message.User.Character.ID,
                message.User.Character.Name,
                message.User.Character.Job,
                message.User.Character.Level,
                message.User.StageUser.Context.Options.ChannelID,
                message.User.Field?.ID ?? 999999999,
                message.GuildName
            )
        );

        if (createResponse.Result != GuildResult.Success)
        {
            var errOpcode = createResponse.Result switch
            {
                GuildResult.FailedAlreadyInGuild =>
                    GuildResultOperations.CreateNewGuild_AlreadyJoined,
                GuildResult.FailedNameTaken =>
                    GuildResultOperations.CreateNewGuild_GuildNameAlreadyExist,
                GuildResult.FailedBeginner => GuildResultOperations.CreateNewGuild_Beginner,
                _ => GuildResultOperations.CreateNewGuild_Unknown,
            };
            using var err = new PacketWriter(PacketSendOperations.GuildResult);
            err.WriteByte((byte)errOpcode);
            await message.User.Dispatch(err.Build());
            return;
        }

        // Step 6 — deduct the creation fee on success.
        // NotifyGuildCreated (sent by GuildService via the message bus) delivers
        // CreateNewGuild_Done with the full GUILDDATA to the creator.
        if (_options.CreationFee > 0)
            await message.User.ModifyStats(s => s.Money -= _options.CreationFee);
    }
}
