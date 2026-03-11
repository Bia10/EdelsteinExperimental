using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Utilities.Pipelines;
using Foundatio.Messaging;

namespace Edelstein.Common.Gameplay.Game;

/// <summary>
/// Subscribes all guild-related message-bus topics to their corresponding
/// gameplay pipelines when the stage starts. Mirrors the layout of
/// <see cref="NotifyPartyInit"/> for the guild subsystem.
/// </summary>
public class NotifyGuildInit : IPipelinePlug<StageStart>
{
    private readonly IMessageBus _messaging;
    private readonly IPipeline<NotifyGuildCreated> _notifyGuildCreated;
    private readonly IPipeline<NotifyGuildDisbanded> _notifyGuildDisbanded;
    private readonly IPipeline<NotifyGuildMemberInvited> _notifyGuildMemberInvited;
    private readonly IPipeline<NotifyGuildInviteRejected> _notifyGuildInviteRejected;
    private readonly IPipeline<NotifyGuildMemberJoined> _notifyGuildMemberJoined;
    private readonly IPipeline<NotifyGuildMemberWithdrawn> _notifyGuildMemberWithdrawn;
    private readonly IPipeline<NotifyGuildUpdated> _notifyGuildUpdated;
    private readonly IPipeline<NotifyGuildMemberLevelOrJobChanged> _notifyGuildMemberLevelOrJobChanged;
    private readonly IPipeline<NotifyGuildMemberOnlineChanged> _notifyGuildMemberOnlineChanged;
    private readonly IPipeline<NotifyGuildNoticeChanged> _notifyGuildNoticeChanged;
    private readonly IPipeline<NotifyGuildGradeNamesChanged> _notifyGuildGradeNamesChanged;
    private readonly IPipeline<NotifyGuildMemberGradeChanged> _notifyGuildMemberGradeChanged;
    private readonly IPipeline<NotifyGuildMarkChanged> _notifyGuildMarkChanged;
    private readonly IPipeline<NotifyGuildSkillUpdated> _notifyGuildSkillUpdated;
    private readonly IPipeline<NotifyGuildMaxMemberChanged> _notifyGuildMaxMemberChanged;

    public NotifyGuildInit(
        IMessageBus messaging,
        IPipeline<NotifyGuildCreated> notifyGuildCreated,
        IPipeline<NotifyGuildDisbanded> notifyGuildDisbanded,
        IPipeline<NotifyGuildMemberInvited> notifyGuildMemberInvited,
        IPipeline<NotifyGuildInviteRejected> notifyGuildInviteRejected,
        IPipeline<NotifyGuildMemberJoined> notifyGuildMemberJoined,
        IPipeline<NotifyGuildMemberWithdrawn> notifyGuildMemberWithdrawn,
        IPipeline<NotifyGuildUpdated> notifyGuildUpdated,
        IPipeline<NotifyGuildMemberLevelOrJobChanged> notifyGuildMemberLevelOrJobChanged,
        IPipeline<NotifyGuildMemberOnlineChanged> notifyGuildMemberOnlineChanged,
        IPipeline<NotifyGuildNoticeChanged> notifyGuildNoticeChanged,
        IPipeline<NotifyGuildGradeNamesChanged> notifyGuildGradeNamesChanged,
        IPipeline<NotifyGuildMemberGradeChanged> notifyGuildMemberGradeChanged,
        IPipeline<NotifyGuildMarkChanged> notifyGuildMarkChanged,
        IPipeline<NotifyGuildSkillUpdated> notifyGuildSkillUpdated,
        IPipeline<NotifyGuildMaxMemberChanged> notifyGuildMaxMemberChanged
    )
    {
        _messaging = messaging;
        _notifyGuildCreated = notifyGuildCreated;
        _notifyGuildDisbanded = notifyGuildDisbanded;
        _notifyGuildMemberInvited = notifyGuildMemberInvited;
        _notifyGuildInviteRejected = notifyGuildInviteRejected;
        _notifyGuildMemberJoined = notifyGuildMemberJoined;
        _notifyGuildMemberWithdrawn = notifyGuildMemberWithdrawn;
        _notifyGuildUpdated = notifyGuildUpdated;
        _notifyGuildMemberLevelOrJobChanged = notifyGuildMemberLevelOrJobChanged;
        _notifyGuildMemberOnlineChanged = notifyGuildMemberOnlineChanged;
        _notifyGuildNoticeChanged = notifyGuildNoticeChanged;
        _notifyGuildGradeNamesChanged = notifyGuildGradeNamesChanged;
        _notifyGuildMemberGradeChanged = notifyGuildMemberGradeChanged;
        _notifyGuildMarkChanged = notifyGuildMarkChanged;
        _notifyGuildSkillUpdated = notifyGuildSkillUpdated;
        _notifyGuildMaxMemberChanged = notifyGuildMaxMemberChanged;
    }

    public async Task Handle(IPipelineContext ctx, StageStart message)
    {
        await _messaging.SubscribeAsync<NotifyGuildCreated>(
            e => _notifyGuildCreated.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildDisbanded>(
            e => _notifyGuildDisbanded.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildMemberInvited>(
            e => _notifyGuildMemberInvited.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildInviteRejected>(
            e => _notifyGuildInviteRejected.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildMemberJoined>(
            e => _notifyGuildMemberJoined.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildMemberWithdrawn>(
            e => _notifyGuildMemberWithdrawn.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildUpdated>(
            e => _notifyGuildUpdated.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildMemberLevelOrJobChanged>(
            e => _notifyGuildMemberLevelOrJobChanged.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildMemberOnlineChanged>(
            e => _notifyGuildMemberOnlineChanged.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildNoticeChanged>(
            e => _notifyGuildNoticeChanged.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildGradeNamesChanged>(
            e => _notifyGuildGradeNamesChanged.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildMemberGradeChanged>(
            e => _notifyGuildMemberGradeChanged.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildMarkChanged>(
            e => _notifyGuildMarkChanged.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildSkillUpdated>(
            e => _notifyGuildSkillUpdated.Process(e)
        );
        await _messaging.SubscribeAsync<NotifyGuildMaxMemberChanged>(
            e => _notifyGuildMaxMemberChanged.Process(e)
        );
    }
}
