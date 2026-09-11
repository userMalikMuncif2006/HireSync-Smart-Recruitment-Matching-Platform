using HireSync.Application.DTOs;
using HireSync.Application.DTOs.Matching;
using HireSync.Application.Interfaces.Matching;
using HireSync.Domain.Enums;
using HireSync.Domain.Matching;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Matching;

public sealed class JobMatchService : IJobMatchService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly IMatchEngine _matchEngine;

    public JobMatchService(
        HireSyncDbContext dbContext,
        IMatchEngine matchEngine)
    {
        _dbContext = dbContext;
        _matchEngine = matchEngine;
    }

    public async Task<JobMatchQueryResult> GetMatchAsync(
        Guid jobSeekerUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty ||
            vacancyId == Guid.Empty)
        {
            return JobMatchQueryResult.Failure(
                JobMatchFailureReason.InvalidInput);
        }

        var profile =
            await (
                from jobSeekerProfile in
                    _dbContext.JobSeekerProfiles.AsNoTracking()

                join user in
                    _dbContext.Users.AsNoTracking()
                    on jobSeekerProfile.UserId equals user.Id

                where
                    jobSeekerProfile.UserId == jobSeekerUserId &&
                    user.AccountStatus == AccountStatus.Active

                select jobSeekerProfile)
            .SingleOrDefaultAsync(
                cancellationToken);

        if (profile is null)
        {
            return JobMatchQueryResult.Failure(
                JobMatchFailureReason.JobSeekerNotReady);
        }

        var candidateSkills =
            await (
                from jobSeekerSkill in
                    _dbContext.JobSeekerSkills.AsNoTracking()

                join skill in
                    _dbContext.Skills.AsNoTracking()
                    on jobSeekerSkill.SkillId equals skill.Id

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

        if (!profile.IsMatchReady(
                candidateSkills.Count > 0))
        {
            return JobMatchQueryResult.Failure(
                JobMatchFailureReason.JobSeekerNotReady);
        }

        var vacancy =
            await (
                from candidate in
                    _dbContext.Vacancies.AsNoTracking()

                join employerProfile in
                    _dbContext.EmployerProfiles.AsNoTracking()
                    on candidate.EmployerProfileId
                    equals employerProfile.Id

                join employerUser in
                    _dbContext.Users.AsNoTracking()
                    on employerProfile.UserId
                    equals employerUser.Id

                where
                    candidate.Id == vacancyId &&
                    candidate.Status == VacancyStatus.Open &&

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

                select candidate)
            .SingleOrDefaultAsync(
                cancellationToken);

        if (vacancy is null)
        {
            return JobMatchQueryResult.Failure(
                JobMatchFailureReason.VacancyUnavailable);
        }

        var requiredSkills =
            await (
                from vacancySkill in
                    _dbContext.VacancySkills.AsNoTracking()

                join skill in
                    _dbContext.Skills.AsNoTracking()
                    on vacancySkill.SkillId equals skill.Id

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
            return JobMatchQueryResult.Failure(
                JobMatchFailureReason.VacancyUnavailable);
        }

        var candidateInput =
            new CandidateMatchInput(
                candidateSkills,
                profile.TotalExperienceMonths!.Value,
                profile.EducationLevel!.Value,
                profile.NormalizedPreferredLocation!);

        var vacancyInput =
            new VacancyMatchInput(
                requiredSkills,
                vacancy.MinimumExperienceMonths,
                vacancy.RequiredEducationLevel,
                vacancy.NormalizedLocation);

        var result =
            _matchEngine.Calculate(
                candidateInput,
                vacancyInput);

        var dto =
            new MatchResultDto(
                result.TotalScore,
                result.SkillsScore,
                result.ExperienceScore,
                result.EducationScore,
                result.LocationScore,
                result.MatchedSkills
                    .Select(skill =>
                        new SkillSummaryDto(
                            skill.Id,
                            skill.Name))
                    .ToArray(),
                result.MissingSkills
                    .Select(skill =>
                        new SkillSummaryDto(
                            skill.Id,
                            skill.Name))
                    .ToArray());

        return JobMatchQueryResult.Success(dto);
    }
}
