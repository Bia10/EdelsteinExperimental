using System.Collections.Immutable;
using Edelstein.Common.Gameplay.Handling;
using Edelstein.Common.Gameplay.Social;
using Edelstein.Common.Utilities.Packets;
using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Gameplay.Game;
using Edelstein.Protocol.Utilities.Pipelines;

namespace Edelstein.Common.Gameplay.Game.Handling.Plugs;

/// <summary>
/// Handles the <c>NotifyGuildMemberWithdrawn</c> message-bus event.
/// Sends either <c>WithdrawGuild_Done (0x2E)</c> or <c>KickGuild_Done (0x31)</c>
/// to all online guild members, including the departing character.
/// Wire layout (V95 §11 cases 46 / 49):
/// guildID(4) + charID(4) + charName(str).
/// The departing user's <see cref="IGameStageUser.Guild"/> is cleared.
/// </summary>
public class NotifyGuildMemberWithdrawnPlug : IPipelinePlug<NotifyGuildMemberWithdrawn>
{
    private readonly IGameStage _stage;

    public NotifyGuildMemberWithdrawnPlug(IGameStage stage) => _stage = stage;

    public async Task Handle(IPipelineContext ctx, NotifyGuildMemberWithdrawn message)
    {
        var users = await _stage.Users.RetrieveAll();
        var affected = users.Where(u => u.Guild?.ID == message.GuildID).ToImmutableArray();

        var opcode = message.IsKicked
            ? GuildResultOperations.KickGuild_Done
            : GuildResultOperations.WithdrawGuild_Done;

        foreach (var user in affected)
        {
            var isDeparting = user.Character?.ID == message.CharacterID;

            if (isDeparting)
            {
                // Departing member loses guild state.
                user.Guild = null;
            }
            else
            {
                // Remaining members remove the departing entry from their snapshot.
                user.Guild?.Members.Remove(message.CharacterID);
            }

            using var packet = new PacketWriter(PacketSendOperations.GuildResult);
            packet.WriteByte((byte)opcode);
            packet.WriteInt(message.GuildID);
            packet.WriteInt(message.CharacterID);
            packet.WriteString(message.CharacterName);
            _ = user.Dispatch(packet.Build());
        }
    }
}
