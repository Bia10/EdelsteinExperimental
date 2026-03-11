using Edelstein.Common.Services.Social.Entities;
using Edelstein.Protocol.Gameplay.Contracts;
using Edelstein.Protocol.Gameplay.Models.Characters;
using Edelstein.Protocol.Services.Social;
using Edelstein.Protocol.Services.Social.Contracts;
using Foundatio.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Edelstein.Common.Services.Social;

public class GuildService : IGuildService
{
    private readonly GuildOptions _options;
    private readonly IDbContextFactory<SocialDbContext> _dbFactory;
    private readonly IMessageBus _messaging;
    private readonly ICharacterRepository _characterRepository;
    private readonly ILogger<GuildService> _logger;

    public GuildService(
        GuildOptions options,
        IDbContextFactory<SocialDbContext> dbFactory,
        IMessageBus messaging,
        ICharacterRepository characterRepository,
        ILogger<GuildService> logger)
    {
        _options = options;
        _dbFactory = dbFactory;
        _messaging = messaging;
        _characterRepository = characterRepository;
        _logger = logger;
    }


    private static async Task<GuildMembership?> LoadMembershipAsync(
        SocialDbContext db,
        int characterID)
    {
        var entity = await db.GuildMembers
            .Include(m => m.Guild)
                .ThenInclude(g => g.Members)
            .Include(m => m.Guild)
                .ThenInclude(g => g.Skills)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.CharacterID == characterID);

        return entity == null ? null : new GuildMembership(entity);
    }


    public async Task<GuildLoadResponse> Load(GuildLoadRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var membership = await LoadMembershipAsync(db, request.CharacterID);
            return new GuildLoadResponse(GuildResult.Success, membership);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load failed for character {CharacterID}", request.CharacterID);
            return new GuildLoadResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildNameCheckResponse> CheckName(GuildNameCheckRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(request.GuildName)
                || request.GuildName.Length < _options.MinNameLength
                || request.GuildName.Length > _options.MaxNameLength)
                return new GuildNameCheckResponse(GuildResult.FailedNameInvalid);

            var taken = await db.Guilds.AnyAsync(g => g.Name == request.GuildName);
            return new GuildNameCheckResponse(taken
                ? GuildResult.FailedNameTaken
                : GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckName failed for name '{GuildName}'", request.GuildName);
            return new GuildNameCheckResponse(GuildResult.FailedUnknown);
        }
    }


    public async Task<GuildResponse> Create(GuildCreateRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (await db.GuildMembers.AnyAsync(m => m.CharacterID == request.CharacterID))
                return new GuildResponse(GuildResult.FailedAlreadyInGuild);

            if (await db.Guilds.AnyAsync(g => g.Name == request.GuildName))
                return new GuildResponse(GuildResult.FailedNameTaken);

            var guild = new GuildEntity
            {
                Name = request.GuildName,
                MasterCharacterID = request.CharacterID,
                MaxMemberNum = _options.DefaultMaxMemberNum,
                GradeName1 = "Master",
                GradeName2 = "Jr.Master",
                GradeName3 = "Member",
                GradeName4 = "Member",
                GradeName5 = "Member",
            };
            var master = new GuildMemberEntity
            {
                Guild = guild,
                CharacterID = request.CharacterID,
                CharacterName = request.CharacterName,
                Job = request.Job,
                Level = request.Level,
                Grade = 1,
                ChannelID = request.ChannelID,
            };
            guild.Members.Add(master);

            await db.Guilds.AddAsync(guild);
            await db.SaveChangesAsync();

            // Re-load with all includes for full snapshot.
            var membership = await LoadMembershipAsync(db, request.CharacterID);
            if (membership != null)
                await _messaging.PublishAsync(new NotifyGuildCreated(request.CharacterID, membership));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create failed for character {CharacterID} guild '{GuildName}'", request.CharacterID, request.GuildName);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> Disband(GuildDisbandRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var guild = await db.Guilds.FirstOrDefaultAsync(g =>
                g.ID == request.GuildID && g.MasterCharacterID == request.CharacterID);

            if (guild == null)
                return new GuildResponse(GuildResult.FailedNotMaster);

            await db.Guilds
                .Where(g => g.ID == request.GuildID)
                .ExecuteDeleteAsync();

            await _messaging.PublishAsync(new NotifyGuildDisbanded(
                request.CharacterID,
                request.GuildID));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disband failed for guild {GuildID} by character {CharacterID}", request.GuildID, request.CharacterID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }


    public async Task<GuildResponse> Invite(GuildInviteRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;

            // Inviter must be in the guild with grade ≤ 2 (master or jr. master).
            var inviterMember = await db.GuildMembers
                .FirstOrDefaultAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.InviterID &&
                    m.Grade <= 2);

            if (inviterMember == null)
                return new GuildResponse(GuildResult.FailedNotMaster);

            var guild = await db.Guilds
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.ID == request.GuildID);

            if (guild == null)
                return new GuildResponse(GuildResult.FailedGuildNotFound);

            if (guild.Members.Count >= guild.MaxMemberNum)
                return new GuildResponse(GuildResult.FailedFull);

            var target = await _characterRepository.RetrieveByName(request.CharacterName);
            if (target == null)
                return new GuildResponse(GuildResult.FailedCharacterNotFound);

            if (target.ID == request.InviterID)
                return new GuildResponse(GuildResult.FailedSelf);

            if (await db.GuildMembers.AnyAsync(m => m.CharacterID == target.ID))
                return new GuildResponse(GuildResult.FailedAlreadyInGuild);

            if (await db.GuildInvitations.AnyAsync(i =>
                    i.GuildID == request.GuildID &&
                    i.CharacterID == target.ID &&
                    i.DateExpire > now))
                return new GuildResponse(GuildResult.FailedAlreadyInvited);

            // Remove any stale invitation and create a fresh one.
            await db.GuildInvitations
                .Where(i => i.GuildID == request.GuildID && i.CharacterID == target.ID)
                .ExecuteDeleteAsync();

            await db.GuildInvitations.AddAsync(new GuildInvitationEntity
            {
                GuildID = request.GuildID,
                InviterID = request.InviterID,
                CharacterID = target.ID,
                DateExpire = now.AddMinutes(_options.InviteExpiryMinutes),
            });

            await db.SaveChangesAsync();

            await _messaging.PublishAsync(new NotifyGuildMemberInvited(
                request.InviterID,
                request.InviterName,
                request.GuildID,
                guild.Name,
                target.ID));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invite failed for guild {GuildID} inviter {InviterID} target '{CharacterName}'", request.GuildID, request.InviterID, request.CharacterName);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> InviteAccept(GuildInviteAcceptRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;

            // Look up the pending invitation by (InviterID, CharacterID) — this is
            // what the V95 client sends in the JoinGuild (0x06) packet (R-004).
            var invitation = await db.GuildInvitations
                .FirstOrDefaultAsync(i =>
                    i.InviterID == request.InviterID &&
                    i.CharacterID == request.CharacterID);

            if (invitation == null || invitation.DateExpire < now)
                return new GuildResponse(GuildResult.FailedNotInvited);

            if (await db.GuildMembers.AnyAsync(m => m.CharacterID == request.CharacterID))
                return new GuildResponse(GuildResult.FailedAlreadyInGuild);

            var guild = await db.Guilds
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.ID == invitation.GuildID);

            if (guild == null)
                return new GuildResponse(GuildResult.FailedGuildNotFound);

            if (guild.Members.Count >= guild.MaxMemberNum)
                return new GuildResponse(GuildResult.FailedFull);

            var newMember = new GuildMemberEntity
            {
                GuildID = invitation.GuildID,
                CharacterID = request.CharacterID,
                CharacterName = request.CharacterName,
                Job = request.Job,
                Level = request.Level,
                Grade = 5,
                ChannelID = request.ChannelID,
            };

            db.GuildInvitations.Remove(invitation);
            await db.GuildMembers.AddAsync(newMember);
            await db.SaveChangesAsync();

            // Reload for complete snapshot.
            var membership = await LoadMembershipAsync(db, request.CharacterID);
            if (membership != null)
                await _messaging.PublishAsync(new NotifyGuildMemberJoined(
                    invitation.GuildID,
                    membership,
                    new GuildMembershipMember(newMember)));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InviteAccept failed for character {CharacterID} inviter {InviterID}", request.CharacterID, request.InviterID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> InviteReject(GuildInviteRejectRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var invitation = await db.GuildInvitations
                .FirstOrDefaultAsync(i => i.CharacterID == request.CharacterID);

            if (invitation == null)
                return new GuildResponse(GuildResult.FailedNotInvited);

            var inviterID = invitation.InviterID;

            db.GuildInvitations.Remove(invitation);
            await db.SaveChangesAsync();

            // Notify the inviter that their invite was declined.
            await _messaging.PublishAsync(new NotifyGuildInviteRejected(
                inviterID,
                request.CharacterName,
                request.IsAlreadyInvited));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InviteReject failed for character {CharacterID}", request.CharacterID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> Leave(GuildLeaveRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var guild = await db.Guilds
                .FirstOrDefaultAsync(g => g.ID == request.GuildID);

            if (guild == null)
                return new GuildResponse(GuildResult.FailedGuildNotFound);

            if (guild.MasterCharacterID == request.CharacterID)
                return new GuildResponse(GuildResult.FailedNotMaster);

            var member = await db.GuildMembers
                .FirstOrDefaultAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID);

            if (member == null)
                return new GuildResponse(GuildResult.FailedNotInGuild);

            db.GuildMembers.Remove(member);
            await db.SaveChangesAsync();

            await _messaging.PublishAsync(new NotifyGuildMemberWithdrawn(
                request.GuildID,
                request.CharacterID,
                member.CharacterName,
                false));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Leave failed for guild {GuildID} character {CharacterID}", request.GuildID, request.CharacterID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> Kick(GuildKickRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (request.MasterID == request.CharacterID)
                return new GuildResponse(GuildResult.FailedSelf);

            var requester = await db.GuildMembers
                .FirstOrDefaultAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.MasterID &&
                    m.Grade <= 2);

            if (requester == null)
                return new GuildResponse(GuildResult.FailedNotMaster);

            var target = await db.GuildMembers
                .FirstOrDefaultAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID);

            if (target == null)
                return new GuildResponse(GuildResult.FailedNotInGuild);

            // Jr. Master (grade 2) may not kick the Master or other Jr. Masters.
            if (requester.Grade == 2 && target.Grade <= 2)
                return new GuildResponse(GuildResult.FailedNotMaster);

            db.GuildMembers.Remove(target);
            await db.SaveChangesAsync();

            await _messaging.PublishAsync(new NotifyGuildMemberWithdrawn(
                request.GuildID,
                request.CharacterID,
                target.CharacterName,
                true));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kick failed for guild {GuildID} master {MasterID} target {CharacterID}", request.GuildID, request.MasterID, request.CharacterID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }


    public async Task<GuildResponse> SetNotice(GuildSetNoticeRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (!await db.GuildMembers.AnyAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID &&
                    m.Grade <= 2))
                return new GuildResponse(GuildResult.FailedNotMaster);

            var updated = await db.Guilds
                .Where(g => g.ID == request.GuildID)
                .ExecuteUpdateAsync(g => g.SetProperty(e => e.Notice, request.Notice));

            if (updated == 0)
                return new GuildResponse(GuildResult.FailedGuildNotFound);

            await _messaging.PublishAsync(new NotifyGuildNoticeChanged(
                request.GuildID,
                request.Notice));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetNotice failed for guild {GuildID}", request.GuildID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> SetGradeNames(GuildSetGradeNamesRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (!await db.GuildMembers.AnyAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID &&
                    m.Grade == 1))
                return new GuildResponse(GuildResult.FailedNotMaster);

            var updated = await db.Guilds
                .Where(g => g.ID == request.GuildID)
                .ExecuteUpdateAsync(g => g
                    .SetProperty(e => e.GradeName1, request.GradeName1)
                    .SetProperty(e => e.GradeName2, request.GradeName2)
                    .SetProperty(e => e.GradeName3, request.GradeName3)
                    .SetProperty(e => e.GradeName4, request.GradeName4)
                    .SetProperty(e => e.GradeName5, request.GradeName5));

            if (updated == 0)
                return new GuildResponse(GuildResult.FailedGuildNotFound);

            await _messaging.PublishAsync(new NotifyGuildGradeNamesChanged(
                request.GuildID,
                [request.GradeName1, request.GradeName2, request.GradeName3, request.GradeName4, request.GradeName5]));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetGradeNames failed for guild {GuildID}", request.GuildID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> SetMemberGrade(GuildSetMemberGradeRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Only the master (grade 1) may change grades; master cannot change own grade.
            if (!await db.GuildMembers.AnyAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.MasterID &&
                    m.Grade == 1))
                return new GuildResponse(GuildResult.FailedNotMaster);

            if (request.CharacterID == request.MasterID)
                return new GuildResponse(GuildResult.FailedSelf);

            // Grade 5 = deepest non-master tier; Grade 2+ reserved (don't promote to master).
            if (request.Grade is < 2 or > 5)
                return new GuildResponse(GuildResult.FailedUnknown);

            var updated = await db.GuildMembers
                .Where(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID)
                .ExecuteUpdateAsync(m => m.SetProperty(e => e.Grade, request.Grade));

            if (updated == 0)
                return new GuildResponse(GuildResult.FailedNotInGuild);

            await _messaging.PublishAsync(new NotifyGuildMemberGradeChanged(
                request.GuildID,
                request.CharacterID,
                request.Grade));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetMemberGrade failed for guild {GuildID} character {CharacterID} grade {Grade}", request.GuildID, request.CharacterID, request.Grade);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> SetMark(GuildSetMarkRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (!await db.GuildMembers.AnyAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID &&
                    m.Grade == 1))
                return new GuildResponse(GuildResult.FailedNotMaster);

            var updated = await db.Guilds
                .Where(g => g.ID == request.GuildID)
                .ExecuteUpdateAsync(g => g
                    .SetProperty(e => e.MarkBg, request.MarkBg)
                    .SetProperty(e => e.MarkBgColor, request.MarkBgColor)
                    .SetProperty(e => e.Mark, request.Mark)
                    .SetProperty(e => e.MarkColor, request.MarkColor));

            if (updated == 0)
                return new GuildResponse(GuildResult.FailedGuildNotFound);

            await _messaging.PublishAsync(new NotifyGuildMarkChanged(
                request.GuildID,
                request.MarkBg,
                request.MarkBgColor,
                request.Mark,
                request.MarkColor));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetMark failed for guild {GuildID}", request.GuildID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> IncMaxMemberNum(GuildIncMaxMemberRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var guild = await db.Guilds
                .FirstOrDefaultAsync(g =>
                    g.ID == request.GuildID &&
                    g.MasterCharacterID == request.CharacterID);

            if (guild == null)
                return new GuildResponse(GuildResult.FailedNotMaster);

            if (guild.MaxMemberNum >= _options.MaxMemberNum)
                return new GuildResponse(GuildResult.FailedUnknown);

            var newMax = Math.Min(guild.MaxMemberNum + _options.ExpandStep, _options.MaxMemberNum);

            await db.Guilds
                .Where(g => g.ID == request.GuildID)
                .ExecuteUpdateAsync(g => g.SetProperty(e => e.MaxMemberNum, newMax));

            await _messaging.PublishAsync(new NotifyGuildMaxMemberChanged(request.GuildID, newMax));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IncMaxMemberNum failed for guild {GuildID} character {CharacterID}", request.GuildID, request.CharacterID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }


    public async Task<GuildResponse> UpdateLevelOrJob(GuildUpdateLevelOrJobRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var member = await db.GuildMembers
                .FirstOrDefaultAsync(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID);

            if (member == null)
                return new GuildResponse(GuildResult.FailedNotInGuild);

            await db.GuildMembers
                .Where(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(e => e.Level, request.Level)
                    .SetProperty(e => e.Job, request.Job));

            await _messaging.PublishAsync(new NotifyGuildMemberLevelOrJobChanged(
                request.GuildID,
                request.CharacterID,
                request.Level,
                request.Job));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateLevelOrJob failed for guild {GuildID} character {CharacterID}", request.GuildID, request.CharacterID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }

    public async Task<GuildResponse> UpdateChannel(GuildUpdateChannelRequest request)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            await db.GuildMembers
                .Where(m =>
                    m.GuildID == request.GuildID &&
                    m.CharacterID == request.CharacterID)
                .ExecuteUpdateAsync(m => m.SetProperty(e => e.ChannelID, request.ChannelID));

            await _messaging.PublishAsync(new NotifyGuildMemberOnlineChanged(
                request.GuildID,
                request.CharacterID,
                request.ChannelID >= 0));

            return new GuildResponse(GuildResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateChannel failed for guild {GuildID} character {CharacterID} channel {ChannelID}", request.GuildID, request.CharacterID, request.ChannelID);
            return new GuildResponse(GuildResult.FailedUnknown);
        }
    }
}
