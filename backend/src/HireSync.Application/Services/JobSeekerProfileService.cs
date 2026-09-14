using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.JobSeeker;
using HireSync.Application.Interfaces.Persistence;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Application.Services;

public sealed class JobSeekerProfileService : IJobSeekerProfileService
{
    private readonly IHireSyncDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public JobSeekerProfileService(
        IHireSyncDbContext dbContext,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<JobSeekerProfileDto?> GetOwnProfileAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetJobSeekerUserId(out var userId))
        {
            return null;
        }

        var profile = await _dbContext.JobSeekerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var skillIds = await _dbContext.JobSeekerSkills
            .AsNoTracking()
            .Where(
                link =>
                    link.JobSeekerProfileId == profile.Id)
            .Select(link => link.SkillId)
            .ToListAsync(cancellationToken);

        var skills = await _dbContext.Skills
            .AsNoTracking()
            .Where(skill => skillIds.Contains(skill.Id))
            .OrderBy(skill => skill.Name)
            .Select(
                skill =>
                    new SkillSummaryDto(
                        skill.Id,
                        skill.Name))
            .ToListAsync(cancellationToken);

        return ToDto(profile, skills);
    }

    public async Task<JobSeekerProfileUpdateResult> UpdateOwnProfileAsync(
        UpdateJobSeekerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetJobSeekerUserId(out var userId))
        {
            return JobSeekerProfileUpdateResult.Failure(
                JobSeekerProfileUpdateFailureReason
                    .InvalidAuthenticatedUser);
        }

        if (request is null ||
            request.ExperienceMonths < 0 ||
            request.ExperienceMonths >
                JobSeekerProfile.MaxExperienceMonths ||
            string.IsNullOrWhiteSpace(
                request.PreferredLocation) ||
            request.SkillIds is null)
        {
            return JobSeekerProfileUpdateResult.Failure(
                JobSeekerProfileUpdateFailureReason
                    .InvalidInput);
        }

        var requestedSkillIds = request.SkillIds
            .Distinct()
            .ToArray();

        if (requestedSkillIds.Any(
                skillId => skillId == Guid.Empty))
        {
            return JobSeekerProfileUpdateResult.Failure(
                JobSeekerProfileUpdateFailureReason
                    .InvalidInput);
        }

        var canonicalSkills = await _dbContext.Skills
            .AsNoTracking()
            .Where(
                skill =>
                    requestedSkillIds.Contains(skill.Id))
            .OrderBy(skill => skill.Name)
            .Select(
                skill =>
                    new SkillSummaryDto(
                        skill.Id,
                        skill.Name))
            .ToListAsync(cancellationToken);

        if (canonicalSkills.Count !=
            requestedSkillIds.Length)
        {
            return JobSeekerProfileUpdateResult.Failure(
                JobSeekerProfileUpdateFailureReason
                    .SkillNotFound);
        }

        var profile = await _dbContext.JobSeekerProfiles
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId,
                cancellationToken);

        var now = _clock.UtcNow;

        try
        {
            if (profile is null)
            {
                profile = new JobSeekerProfile(
                    Guid.NewGuid(),
                    userId,
                    now);

                _dbContext.JobSeekerProfiles.Add(
                    profile);
            }

            profile.UpdateStructuredProfile(
                request.ExperienceMonths,
                request.EducationLevel,
                request.PreferredLocation,
                now);

            var existingSkillLinks =
                await _dbContext.JobSeekerSkills
                    .Where(
                        link =>
                            link.JobSeekerProfileId ==
                            profile.Id)
                    .ToListAsync(cancellationToken);

            var requestedSkillIdSet =
                requestedSkillIds.ToHashSet();

            var linksToRemove =
                existingSkillLinks
                    .Where(
                        link =>
                            !requestedSkillIdSet.Contains(
                                link.SkillId))
                    .ToArray();

            if (linksToRemove.Length > 0)
            {
                _dbContext.JobSeekerSkills
                    .RemoveRange(linksToRemove);
            }

            var existingSkillIdSet =
                existingSkillLinks
                    .Select(link => link.SkillId)
                    .ToHashSet();

            var linksToAdd =
                requestedSkillIds
                    .Where(
                        skillId =>
                            !existingSkillIdSet.Contains(
                                skillId))
                    .Select(
                        skillId =>
                            new JobSeekerSkill(
                                profile.Id,
                                skillId))
                    .ToArray();

            if (linksToAdd.Length > 0)
            {
                _dbContext.JobSeekerSkills
                    .AddRange(linksToAdd);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (ArgumentException)
        {
            return JobSeekerProfileUpdateResult.Failure(
                JobSeekerProfileUpdateFailureReason
                    .InvalidInput);
        }
        catch (DbUpdateException)
        {
            return JobSeekerProfileUpdateResult.Failure(
                JobSeekerProfileUpdateFailureReason
                    .PersistenceFailed);
        }

        return JobSeekerProfileUpdateResult.Success(
            ToDto(
                profile,
                canonicalSkills));
    }

    private bool TryGetJobSeekerUserId(
        out Guid userId)
    {
        userId = Guid.Empty;

        if (!_currentUser.IsAuthenticated ||
            _currentUser.UserId is not Guid currentUserId ||
            currentUserId == Guid.Empty ||
            !string.Equals(
                _currentUser.Role,
                RoleNames.JobSeeker,
                StringComparison.Ordinal))
        {
            return false;
        }

        userId = currentUserId;
        return true;
    }

    private static JobSeekerProfileDto ToDto(
        JobSeekerProfile profile,
        IReadOnlyList<SkillSummaryDto> skills)
    {
        return new JobSeekerProfileDto(
            profile.TotalExperienceMonths,
            profile.EducationLevel,
            profile.PreferredLocation,
            skills,
            profile.IsMatchReady(
                skills.Count > 0));
    }
}
