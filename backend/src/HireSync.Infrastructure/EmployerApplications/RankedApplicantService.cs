using HireSync.Application.DTOs;
using HireSync.Application.DTOs.EmployerApplications;
using HireSync.Application.DTOs.Matching;
using HireSync.Application.Interfaces.EmployerApplications;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Domain.Matching;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.EmployerApplications;

public sealed class RankedApplicantService
    : IRankedApplicantService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly IMatchEngine _matchEngine;

    public RankedApplicantService(
        HireSyncDbContext dbContext,
        IMatchEngine matchEngine)
    {
        _dbContext = dbContext;
        _matchEngine = matchEngine;
    }

    public async Task<RankedApplicantQueryResult>
        GetOwnVacancyApplicantsAsync(
            Guid employerUserId,
            Guid vacancyId,
            RankedApplicantListRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (employerUserId == Guid.Empty ||
            vacancyId == Guid.Empty ||
            request is null ||
            !RankedApplicantListRules.IsValid(request))
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason.InvalidInput);
        }

        var employerUser =
            await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == employerUserId,
                    cancellationToken);

        if (employerUser is null ||
            employerUser.AccountStatus != AccountStatus.Active ||
            employerUser.EmployerVerificationStatus !=
                EmployerVerificationStatus.Approved)
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason.VacancyNotFound);
        }

        var employerRoles =
            await (
                from userRole in
                    _dbContext.UserRoles.AsNoTracking()

                join role in
                    _dbContext.Roles.AsNoTracking()
                    on userRole.RoleId
                    equals role.Id

                where
                    userRole.UserId == employerUserId

                select role.Name)
            .ToListAsync(
                cancellationToken);

        if (employerRoles.Count != 1 ||
            !string.Equals(
                employerRoles[0],
                RoleNames.Employer,
                StringComparison.Ordinal))
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason.VacancyNotFound);
        }

        var employerProfile =
            await _dbContext.EmployerProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    profile =>
                        profile.UserId == employerUserId,
                    cancellationToken);

        if (employerProfile is null ||
            !employerProfile.IsProfileComplete)
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason.VacancyNotFound);
        }

        var vacancy =
            await _dbContext.Vacancies
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == vacancyId &&
                        candidate.EmployerProfileId ==
                            employerProfile.Id,
                    cancellationToken);

        if (vacancy is null)
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason.VacancyNotFound);
        }

        var requiredSkills =
            await (
                from vacancySkill in
                    _dbContext.VacancySkills.AsNoTracking()

                join skill in
                    _dbContext.Skills.AsNoTracking()
                    on vacancySkill.SkillId
                    equals skill.Id

                where
                    vacancySkill.VacancyId == vacancy.Id

                orderby
                    skill.NormalizedName,
                    skill.Id

                select new MatchSkill(
                    skill.Id,
                    skill.Name,
                    skill.NormalizedName))
            .ToListAsync(
                cancellationToken);

        if (requiredSkills.Count == 0)
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason
                    .InvalidMatchingData);
        }

        VacancyMatchInput vacancyInput;

        try
        {
            vacancyInput =
                new VacancyMatchInput(
                    requiredSkills,
                    vacancy.MinimumExperienceMonths,
                    vacancy.RequiredEducationLevel,
                    vacancy.NormalizedLocation);
        }
        catch (ArgumentException)
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason
                    .InvalidMatchingData);
        }

        var applicationQuery =
            _dbContext.JobApplications
                .AsNoTracking()
                .Where(
                    application =>
                        application.VacancyId == vacancy.Id);

        if (request.Status.HasValue)
        {
            applicationQuery =
                applicationQuery.Where(
                    application =>
                        application.Status ==
                            request.Status.Value);
        }

        var totalCount =
            await applicationQuery.CountAsync(
                cancellationToken);

        if (totalCount == 0)
        {
            return RankedApplicantQueryResult.Success(
                new RankedApplicantPageDto(
                    vacancy.Id,
                    vacancy.Title,
                    Array.Empty<RankedApplicantDto>(),
                    request.Page,
                    request.PageSize,
                    0));
        }

        var applications =
            await applicationQuery
                .ToListAsync(
                    cancellationToken);

        var profileIds =
            applications
                .Select(
                    application =>
                        application.JobSeekerProfileId)
                .Distinct()
                .ToArray();

        var profiles =
            await _dbContext.JobSeekerProfiles
                .AsNoTracking()
                .Where(
                    profile =>
                        profileIds.Contains(profile.Id))
                .ToListAsync(
                    cancellationToken);

        if (profiles.Count != profileIds.Length)
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason
                    .InvalidMatchingData);
        }

        var profilesById =
            profiles.ToDictionary(
                profile => profile.Id);

        var applicantUserIds =
            profiles
                .Select(
                    profile => profile.UserId)
                .Distinct()
                .ToArray();

        var applicantUsers =
            await _dbContext.Users
                .AsNoTracking()
                .Where(
                    user =>
                        applicantUserIds.Contains(user.Id))
                .ToListAsync(
                    cancellationToken);

        if (applicantUsers.Count != applicantUserIds.Length)
        {
            return RankedApplicantQueryResult.Failure(
                RankedApplicantQueryFailureReason
                    .InvalidMatchingData);
        }

        var applicantUsersById =
            applicantUsers.ToDictionary(
                user => user.Id);

        var candidateSkillRows =
            await (
                from jobSeekerSkill in
                    _dbContext.JobSeekerSkills.AsNoTracking()

                join skill in
                    _dbContext.Skills.AsNoTracking()
                    on jobSeekerSkill.SkillId
                    equals skill.Id

                where
                    profileIds.Contains(
                        jobSeekerSkill.JobSeekerProfileId)

                select new
                {
                    jobSeekerSkill.JobSeekerProfileId,
                    SkillId = skill.Id,
                    skill.Name,
                    skill.NormalizedName
                })
            .ToListAsync(
                cancellationToken);

        var candidateSkillsByProfileId =
            candidateSkillRows
                .GroupBy(
                    row =>
                        row.JobSeekerProfileId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        group
                            .OrderBy(
                                row =>
                                    row.NormalizedName,
                                StringComparer.Ordinal)
                            .ThenBy(
                                row =>
                                    row.SkillId)
                            .Select(
                                row =>
                                    new MatchSkill(
                                        row.SkillId,
                                        row.Name,
                                        row.NormalizedName))
                            .ToArray());

        var applicationIds =
            applications
                .Select(
                    application =>
                        application.Id)
                .ToArray();

        var contactRows =
            await _dbContext.ContactRequests
                .AsNoTracking()
                .Where(
                    contactRequest =>
                        applicationIds.Contains(
                            contactRequest.JobApplicationId))
                .Select(
                    contactRequest =>
                        new
                        {
                            contactRequest.JobApplicationId,
                            contactRequest.Status
                        })
                .ToListAsync(
                    cancellationToken);

        var contactStatusByApplicationId =
            contactRows.ToDictionary(
                row => row.JobApplicationId,
                row => row.Status);

        var rankedCandidates =
            new List<RankedCandidate>(
                applications.Count);

        foreach (var application in applications)
        {
            if (!profilesById.TryGetValue(
                    application.JobSeekerProfileId,
                    out var profile) ||
                !applicantUsersById.TryGetValue(
                    profile.UserId,
                    out var applicantUser) ||
                string.IsNullOrWhiteSpace(
                    applicantUser.DisplayName))
            {
                return RankedApplicantQueryResult.Failure(
                    RankedApplicantQueryFailureReason
                        .InvalidMatchingData);
            }

            var candidateSkills =
                candidateSkillsByProfileId.TryGetValue(
                    profile.Id,
                    out var storedSkills)
                    ? storedSkills
                    : Array.Empty<MatchSkill>();

            if (!profile.IsMatchReady(
                    candidateSkills.Length > 0))
            {
                return RankedApplicantQueryResult.Failure(
                    RankedApplicantQueryFailureReason
                        .InvalidMatchingData);
            }

            MatchResult matchResult;

            try
            {
                var candidateInput =
                    new CandidateMatchInput(
                        candidateSkills,
                        profile.TotalExperienceMonths!.Value,
                        profile.EducationLevel!.Value,
                        profile.NormalizedPreferredLocation!);

                matchResult =
                    _matchEngine.Calculate(
                        candidateInput,
                        vacancyInput);
            }
            catch (ArgumentException)
            {
                return RankedApplicantQueryResult.Failure(
                    RankedApplicantQueryFailureReason
                        .InvalidMatchingData);
            }

            var matchDto =
                new MatchResultDto(
                    matchResult.TotalScore,
                    matchResult.SkillsScore,
                    matchResult.ExperienceScore,
                    matchResult.EducationScore,
                    matchResult.LocationScore,
                    matchResult.MatchedSkills
                        .Select(
                            skill =>
                                new SkillSummaryDto(
                                    skill.Id,
                                    skill.Name))
                        .ToArray(),
                    matchResult.MissingSkills
                        .Select(
                            skill =>
                                new SkillSummaryDto(
                                    skill.Id,
                                    skill.Name))
                        .ToArray());

            var contactStatus =
                contactStatusByApplicationId.TryGetValue(
                    application.Id,
                    out var storedContactStatus)
                    ? storedContactStatus
                    : (ContactRequestStatus?)null;

            rankedCandidates.Add(
                new RankedCandidate(
                    application.Id,
                    application.JobSeekerProfileId,
                    applicantUser.DisplayName,
                    application.Status,
                    application.AppliedAtUtc,
                    application.UpdatedAtUtc,
                    application.RowVersion.ToArray(),
                    contactStatus,
                    matchDto,
                    application.Id
                        .ToString("N")
                        .ToLowerInvariant()));
        }

        var ordered =
            rankedCandidates
                .OrderByDescending(
                    candidate =>
                        candidate.Match.TotalScore)
                .ThenBy(
                    candidate =>
                        candidate.AppliedAtUtc)
                .ThenBy(
                    candidate =>
                        candidate.CanonicalApplicationId,
                    StringComparer.Ordinal)
                .ToArray();

        var ranked =
            ordered
                .Select(
                    (candidate, index) =>
                        new RankedApplicantDto(
                            index + 1,
                            candidate.ApplicationId,
                            candidate.JobSeekerProfileId,
                            candidate.JobSeekerDisplayName,
                            candidate.Status,
                            candidate.AppliedAtUtc,
                            candidate.UpdatedAtUtc,
                            candidate.RowVersion,
                            candidate.ContactRequestStatus,
                            candidate.Match))
                .ToArray();

        var skipLong =
            (long)(request.Page - 1) *
            request.PageSize;

        IReadOnlyList<RankedApplicantDto> pageItems =
            skipLong > int.MaxValue
                ? Array.Empty<RankedApplicantDto>()
                : ranked
                    .Skip((int)skipLong)
                    .Take(request.PageSize)
                    .ToArray();

        var page =
            new RankedApplicantPageDto(
                vacancy.Id,
                vacancy.Title,
                pageItems,
                request.Page,
                request.PageSize,
                totalCount);

        return RankedApplicantQueryResult.Success(
            page);
    }

    private sealed record RankedCandidate(
        Guid ApplicationId,
        Guid JobSeekerProfileId,
        string JobSeekerDisplayName,
        ApplicationStatus Status,
        DateTime AppliedAtUtc,
        DateTime UpdatedAtUtc,
        byte[] RowVersion,
        ContactRequestStatus? ContactRequestStatus,
        MatchResultDto Match,
        string CanonicalApplicationId);
}