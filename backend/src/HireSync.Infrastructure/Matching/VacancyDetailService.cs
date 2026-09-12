using HireSync.Application.DTOs;
using HireSync.Application.DTOs.Matching;
using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Matching;
using HireSync.Application.Interfaces.Time;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Matching;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Matching;

public sealed class VacancyDetailService
    : IVacancyDetailService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly IMatchEngine _matchEngine;
    private readonly IClock _clock;

    public VacancyDetailService(
        HireSyncDbContext dbContext,
        IMatchEngine matchEngine,
        IClock clock)
    {
        _dbContext = dbContext;
        _matchEngine = matchEngine;
        _clock = clock;
    }

    public async Task<VacancyDetailQueryResult> GetDetailAsync(
        Guid jobSeekerUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty ||
            vacancyId == Guid.Empty)
        {
            return VacancyDetailQueryResult.Failure(
                VacancyDetailFailureReason.InvalidInput);
        }

        var vacancyContext =
            await (
                from vacancy in
                    _dbContext.Vacancies.AsNoTracking()

                join employerProfile in
                    _dbContext.EmployerProfiles.AsNoTracking()
                    on vacancy.EmployerProfileId
                    equals employerProfile.Id

                join employerUser in
                    _dbContext.Users.AsNoTracking()
                    on employerProfile.UserId
                    equals employerUser.Id

                where
                    vacancy.Id == vacancyId &&

                    vacancy.Status ==
                        VacancyStatus.Open &&

                    employerUser.AccountStatus ==
                        AccountStatus.Active &&

                    employerUser.EmployerVerificationStatus ==
                        EmployerVerificationStatus.Approved &&

                    employerProfile.CompanyName.Trim() !=
                        string.Empty &&

                    employerProfile.Description.Trim() !=
                        string.Empty &&

                    employerProfile.Location.Trim() !=
                        string.Empty &&

                    employerProfile.ContactPersonName.Trim() !=
                        string.Empty &&

                    employerProfile.ContactPersonDesignation.Trim() !=
                        string.Empty &&

                    employerProfile.BusinessRegistrationNumber.Trim() !=
                        string.Empty &&

                    employerProfile.MobileNumber.Trim() !=
                        string.Empty

                select new
                {
                    Vacancy = vacancy,
                    EmployerProfile = employerProfile
                })
            .SingleOrDefaultAsync(
                cancellationToken);

        if (vacancyContext is null)
        {
            return VacancyDetailQueryResult.Failure(
                VacancyDetailFailureReason.VacancyUnavailable);
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
                    vacancySkill.VacancyId ==
                        vacancyContext.Vacancy.Id

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
            return VacancyDetailQueryResult.Failure(
                VacancyDetailFailureReason.VacancyUnavailable);
        }

        var profile =
            await (
                from jobSeekerProfile in
                    _dbContext.JobSeekerProfiles.AsNoTracking()

                join user in
                    _dbContext.Users.AsNoTracking()
                    on jobSeekerProfile.UserId
                    equals user.Id

                where
                    jobSeekerProfile.UserId ==
                        jobSeekerUserId &&

                    user.AccountStatus ==
                        AccountStatus.Active

                select jobSeekerProfile)
            .SingleOrDefaultAsync(
                cancellationToken);

        var candidateSkills =
            profile is null
                ? new List<MatchSkill>()
                : await (
                    from jobSeekerSkill in
                        _dbContext.JobSeekerSkills.AsNoTracking()

                    join skill in
                        _dbContext.Skills.AsNoTracking()
                        on jobSeekerSkill.SkillId
                        equals skill.Id

                    where
                        jobSeekerSkill.JobSeekerProfileId ==
                            profile.Id

                    orderby
                        skill.NormalizedName,
                        skill.Id

                    select new MatchSkill(
                        skill.Id,
                        skill.Name,
                        skill.NormalizedName))
                .ToListAsync(
                    cancellationToken);

        var missingProfileFields =
            GetMissingProfileFields(
                profile,
                candidateSkills.Count);

        var hasCurrentCv =
            profile is not null &&
            await _dbContext.CvDocuments
                .AsNoTracking()
                .AnyAsync(
                    cv =>
                        cv.JobSeekerProfileId ==
                        profile.Id,
                    cancellationToken);

        var hasApplied =
            profile is not null &&
            await _dbContext.JobApplications
                .AsNoTracking()
                .AnyAsync(
                    application =>
                        application.VacancyId ==
                            vacancyContext.Vacancy.Id &&
                        application.JobSeekerProfileId ==
                            profile.Id,
                    cancellationToken);

        MatchResultDto? match = null;

        var matchStatus =
            MatchStatusCodes.ProfileIncomplete;

        if (missingProfileFields.Count == 0)
        {
            var candidateInput =
                new CandidateMatchInput(
                    candidateSkills,
                    profile!.TotalExperienceMonths!.Value,
                    profile.EducationLevel!.Value,
                    profile.NormalizedPreferredLocation!);

            var vacancyInput =
                new VacancyMatchInput(
                    requiredSkills,
                    vacancyContext.Vacancy.MinimumExperienceMonths,
                    vacancyContext.Vacancy.RequiredEducationLevel,
                    vacancyContext.Vacancy.NormalizedLocation);

            var matchResult =
                _matchEngine.Calculate(
                    candidateInput,
                    vacancyInput);

            match =
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

            matchStatus =
                MatchStatusCodes.Ready;
        }

        var canApply =
            match is not null &&
            hasCurrentCv &&
            !hasApplied;

        var requiredSkillDtos =
            requiredSkills
                .Select(
                    skill =>
                        new SkillSummaryDto(
                            skill.Id,
                            skill.Name))
                .ToArray();

        var detail =
            new PublicVacancyDetailDto(
                vacancyContext.Vacancy.Id,
                vacancyContext.Vacancy.Title,
                vacancyContext.Vacancy.Description,
                vacancyContext.EmployerProfile.CompanyName,
                vacancyContext.EmployerProfile.Description,
                vacancyContext.EmployerProfile.CompanyWebsite,
                vacancyContext.EmployerProfile.Location,
                vacancyContext.Vacancy.Location,
                vacancyContext.Vacancy.MinimumExperienceMonths,
                vacancyContext.Vacancy.RequiredEducationLevel,
                vacancyContext.Vacancy.PublishedAtUtc,
                requiredSkillDtos,
                matchStatus,
                match,
                missingProfileFields,
                canApply,
                hasApplied,
                _clock.UtcNow);

        return VacancyDetailQueryResult.Success(
            detail);
    }

    private static IReadOnlyList<string>
        GetMissingProfileFields(
            JobSeekerProfile? profile,
            int skillCount)
    {
        var missing =
            new List<string>();

        if (profile is null ||
            !profile.TotalExperienceMonths.HasValue)
        {
            missing.Add(
                nameof(
                    JobSeekerProfile
                        .TotalExperienceMonths));
        }

        if (profile is null ||
            !profile.EducationLevel.HasValue)
        {
            missing.Add(
                nameof(
                    JobSeekerProfile
                        .EducationLevel));
        }

        if (profile is null ||
            string.IsNullOrWhiteSpace(
                profile.NormalizedPreferredLocation))
        {
            missing.Add(
                nameof(
                    JobSeekerProfile
                        .PreferredLocation));
        }

        if (skillCount == 0)
        {
            missing.Add(
                "Skills");
        }

        return missing;
    }
}