using Edelstein.Common.Gameplay.Game;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Protocol.Gameplay.Game.Contracts;
using Edelstein.Protocol.Gameplay.Game.Objects.User;
using Edelstein.Protocol.Services.Social.Contracts;
using Edelstein.Protocol.Utilities.Packets;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Gameplay.Game.Handling.Packets;

/// <summary>
/// Handles inbound <c>CP_GuildRequest (0x95)</c> packets from the client.
/// Dispatches each sub-opcode to its respective pipeline contract or handles
/// it inline when no broadcast is required (level/job push updates).
/// </summary>
public class GuildRequestHandler : AbstractFieldHandler
{
    private readonly ILogger _logger;

    public GuildRequestHandler(ILogger<GuildRequestHandler> logger) => _logger = logger;

    public override short Operation => (short)PacketRecvOperations.GuildRequest;

    protected override Task Handle(IFieldUser user, IPacketReader reader)
    {
        var type = (GuildRequestOperations)reader.ReadByte();

        switch (type)
        {
            // Sent by the client immediately after receiving JoinGuild_Done for
            // self to acknowledge and request the full GUILDDATA packet.
            case GuildRequestOperations.LoadGuild:
                return HandleLoadGuildAsync(user);

            case GuildRequestOperations.CheckGuildName:
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildNameCheckRequest.Process(
                    new FieldOnPacketGuildNameCheckRequest(user, reader.ReadString())
                );

            // R-007: client sends 0x20 (Encode1(32)) + charID(4) + bAgree(1).
            // The guild was already created during the CheckGuildName (0x02) flow.
            // These agree/disagree replies are informational; no further action.
            // Dead sub-opcodes 0x04 (CreateNewGuild) and 0x09 (RemoveGuild)
            // are intentionally absent (see R-003 / R-005).
            case (GuildRequestOperations)0x20:
                reader.ReadInt(); // charID  — discard
                reader.ReadByte(); // bAgree  — discard
                return Task.CompletedTask;

            case GuildRequestOperations.InviteGuild:
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildInviteRequest.Process(
                    new FieldOnPacketGuildInviteRequest(user, reader.ReadString())
                );

            case GuildRequestOperations.JoinGuild:
            {
                // R-004: client sends inviterID(4) + myCharacterID(4).
                // We trust the session for own identity; inviterID resolves the invitation.
                var inviterID = reader.ReadInt();
                reader.ReadInt(); // myCharacterID — trusted from session, always discard
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildJoinRequest.Process(
                    new FieldOnPacketGuildJoinRequest(user, inviterID)
                );
            }

            case GuildRequestOperations.WithdrawGuild:
            {
                // Client sends charID + charName for consistency but we use
                // the authenticated user's identity from the session.
                var _ = reader.ReadInt(); // charID  — ignored; trust the session
                var __ = reader.ReadString(); // charName — ignored; trust the session
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildLeaveRequest.Process(
                    new FieldOnPacketGuildLeaveRequest(user)
                );
            }

            case GuildRequestOperations.KickGuild:
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildKickRequest.Process(
                    new FieldOnPacketGuildKickRequest(
                        user,
                        reader.ReadInt(), // targetCharID
                        reader.ReadString() // targetName
                    )
                );

            case GuildRequestOperations.SetNotice:
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildSetNoticeRequest.Process(
                    new FieldOnPacketGuildSetNoticeRequest(user, reader.ReadString())
                );

            case GuildRequestOperations.SetGradeName:
            {
                var names = new string[5];
                for (var i = 0; i < 5; i++)
                    names[i] = reader.ReadString();
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildSetGradeNamesRequest.Process(
                    new FieldOnPacketGuildSetGradeNamesRequest(user, names)
                );
            }

            case GuildRequestOperations.SetMemberGrade:
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildSetMemberGradeRequest.Process(
                    new FieldOnPacketGuildSetMemberGradeRequest(
                        user,
                        reader.ReadInt(), // targetCharID
                        reader.ReadByte() // new grade (Encode1 on client)
                    )
                );

            case GuildRequestOperations.SetMark:
                return user.StageUser.Context.Pipelines.FieldOnPacketGuildSetMarkRequest.Process(
                    new FieldOnPacketGuildSetMarkRequest(
                        user,
                        reader.ReadShort(), // markBg
                        reader.ReadByte(), // markBgColor
                        reader.ReadShort(), // mark
                        reader.ReadByte() // markColor
                    )
                );

            case GuildRequestOperations.ChangeLevel:
            {
                if (user.StageUser.Guild == null)
                    return Task.CompletedTask;
                var newLevel = reader.ReadInt();
                _ = user.StageUser.Context.Services.Guild.UpdateLevelOrJob(
                    new GuildUpdateLevelOrJobRequest(
                        user.StageUser.Guild.ID,
                        user.Character.ID,
                        newLevel,
                        user.Character.Job
                    )
                );
                return Task.CompletedTask;
            }

            case GuildRequestOperations.ChangeJob:
            {
                if (user.StageUser.Guild == null)
                    return Task.CompletedTask;
                var newJob = reader.ReadInt();
                _ = user.StageUser.Context.Services.Guild.UpdateLevelOrJob(
                    new GuildUpdateLevelOrJobRequest(
                        user.StageUser.Guild.ID,
                        user.Character.ID,
                        user.Character.Level,
                        newJob
                    )
                );
                return Task.CompletedTask;
            }

            default:
                _logger.LogDebug(
                    "Unhandled CP_GuildRequest sub-opcode 0x{Type:X2} from character {Name}",
                    (byte)type,
                    user.Character.Name
                );
                return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles the post-join guild load triggered by <c>GuildRequestOperations.LoadGuild (0x00)</c>.
    /// Loads a fresh snapshot from the guild service, stores it on the stage user,
    /// and dispatches <c>LoadGuild_Done (0x1C)</c> with the full GUILDDATA.
    /// </summary>
    private static async Task HandleLoadGuildAsync(IFieldUser user)
    {
        var response = await user.StageUser.Context.Services.Guild.Load(
            new GuildLoadRequest(user.Character.ID)
        );

        user.StageUser.Guild = response.GuildMembership;
        await user.StageUser.DispatchInitGuild();
    }
}
