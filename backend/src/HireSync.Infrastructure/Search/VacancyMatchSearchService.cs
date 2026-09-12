using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Matching;
using HireSync.Application.Interfaces.Vacancy;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Domain.Matching;
using HireSync.Domain.Rules;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Search;

public sealed class VacancyMatchSearchService
    : IVacancyMatchSearchService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly IMatchEngine _matchEngine;

    public VacancyMatchSearchService(
        HireSyncDbContext dbContext,
        IMatchEngine matchEngine)
    {
        _dbContext = dbContext;
        _matchEngine = matchEngine;
    }

    public async Task<VacancySearchQueryResult>
        SearchOpenVacanciesByMatchAsync(
            Guid jobSeekerUserId,
            SearchVacanciesRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty ||
            request is null ||
            !VacancySearchRules.IsValid(request) ||
            request.Sort != VacancySearchSort.Match)
        {
            return VacancySearchQueryResult.Failure(
                VacancySearchFailureReason.InvalidInput);
        }

        var jobSeekerUser =
            await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == jobSeekerUserId,
                    cancellationToken);

        if (jobSeekerUser is null ||
            jobSeekerUser.AccountStatus !=
                AccountStatus.Active)
        {
            return VacancySearchQueryResult.Failure(
                VacancySearchFailureReason.JobSeekerUnavailable);
        }

        var roles =
            await (
                from userRole in
                    _dbContext.UserRoles.AsNoTracking()

                join role in
                    _dbContext.Roles.AsNoTracking()
                    on userRole.RoleId
                    equals role.Id

                where
                    userRole.UserId == jobSeekerUserId

                select role.Name)
            .ToListAsync(
                cancellationToken);

        if (roles.Count != 1 ||
            !string.Equals(
                roles[0],
                RoleNames.JobSeeker,
                StringComparison.Ordinal))
        {
            return VacancySearchQueryResult.Failure(
                VacancySearchFailureReason.JobSeekerUnavailable);
        }

        var profile =
            await _dbContext.JobSeekerProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.UserId == jobSeekerUserId,
                    cancellationToken);

        if (profile is null)
        {
            return VacancySearchQueryResult.Failure(
                VacancySearchFailureReason.ProfileNotReady);
        }

        var candidateSkills =
            await (
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

        if (!profile.IsMatchReady(
                candidateSkills.Count > 0))
        {
            return VacancySearchQueryResult.Failure(
                VacancySearchFailureReason.ProfileNotReady);
        }

        CandidateMatchInput candidateInput;

        try
        {
            candidateInput =
                new CandidateMatchInput(
                    candidateSkills,
                    profile.TotalExperienceMonths!.Value,
                    profile.EducationLevel!.Value,
                    profile.NormalizedPreferredLocation!);
        }
        catch (ArgumentException)
        {
            return VacancySearchQueryResult.Failure(
                VacancySearchFailureReason.InvalidMatchingData);
        }

        var query =
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
            };

        if (!string.IsNullOrWhiteSpace(
                request.Q))
        {
            var normalizedQuery =
                request.Q
                    .Trim()
                    .ToUpperInvariant();

            query =
                query.Where(
                    candidate =>
                        candidate.Vacancy.Title
                            .ToUpper()
                            .Contains(normalizedQuery) ||

                        candidate.Vacancy.Description
                            .ToUpper()
                            .Contains(normalizedQuery) ||

                        candidate.EmployerProfile.CompanyName
                            .ToUpper()
                            .Contains(normalizedQuery));
        }

        if (!string.IsNullOrWhiteSpace(
                request.Location))
        {
            string normalizedLocation;

            try
            {
                normalizedLocation =
                    LocationNormalizer.Normalize(
                        request.Location);
            }
            catch (ArgumentException)
            {
                return VacancySearchQueryResult.Failure(
                    VacancySearchFailureReason.InvalidInput);
            }

            query =
                query.Where(
                    candidate =>
                        candidate.Vacancy.NormalizedLocation ==
                            normalizedLocation);
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        if (totalCount == 0)
        {
            return VacancySearchQueryResult.Success(
                new PublicVacancyPageDto(
                    Array.Empty<PublicVacancyListItemDto>(),
                    request.Page,
                    request.PageSize,
                    0));
        }

        var vacancies =
            await query
                .Select(
                    candidate =>
                        new
                        {
                            candidate.Vacancy.Id,
                            candidate.Vacancy.Title,
                            candidate.EmployerProfile.CompanyName,
                            candidate.Vacancy.Location,
                            candidate.Vacancy.NormalizedLocation,
                            candidate.Vacancy.MinimumExperienceMonths,
                            candidate.Vacancy.RequiredEducationLevel,
                            candidate.Vacancy.PublishedAtUtc
                        })
                .ToListAsync(
                    cancellationToken);

        var vacancyIds =
            vacancies
                .Select(
                    vacancy =>
                        vacancy.Id)
                .ToArray();

        var requiredSkillRows =
            await (
                from vacancySkill in
                    _dbContext.VacancySkills.AsNoTracking()

                join skill in
                    _dbContext.Skills.AsNoTracking()
                    on vacancySkill.SkillId
                    equals skill.Id

                where
                    vacancyIds.Contains(
                        vacancySkill.VacancyId)

                select new
                {
                    vacancySkill.VacancyId,
                    SkillId = skill.Id,
                    skill.Name,
                    skill.NormalizedName
                })
            .ToListAsync(
                cancellationToken);

        var requiredSkillsByVacancy =
            requiredSkillRows
                .GroupBy(
                    row =>
                        row.VacancyId)
                .ToDictionary(
                    group =>
                        group.Key,
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

        var scored =
            new List<PublicVacancyListItemDto>(
                vacancies.Count);

        foreach (var vacancy in vacancies)
        {
            if (!requiredSkillsByVacancy.TryGetValue(
                    vacancy.Id,
                    out var requiredSkills) ||
                requiredSkills.Length == 0)
            {
                return VacancySearchQueryResult.Failure(
                    VacancySearchFailureReason.InvalidMatchingData);
            }

            try
            {
                var vacancyInput =
                    new VacancyMatchInput(
                        requiredSkills,
                        vacancy.MinimumExperienceMonths,
                        vacancy.RequiredEducationLevel,
                        vacancy.NormalizedLocation);

                var match =
                    _matchEngine.Calculate(
                        candidateInput,
                        vacancyInput);

                scored.Add(
                    new PublicVacancyListItemDto(
                        vacancy.Id,
                        vacancy.Title,
                        vacancy.CompanyName,
                        vacancy.Location,
                        vacancy.MinimumExperienceMonths,
                        vacancy.RequiredEducationLevel,
                        vacancy.PublishedAtUtc,
                        match.TotalScore));
            }
            catch (ArgumentException)
            {
                return VacancySearchQueryResult.Failure(
                    VacancySearchFailureReason.InvalidMatchingData);
            }
        }

        var ordered =
            scored
                .OrderByDescending(
                    vacancy =>
                        vacancy.MatchScore!.Value)
                .ThenByDescending(
                    vacancy =>
                        vacancy.PublishedAtUtc)
                .ThenBy(
                    vacancy =>
                        vacancy.Id)
                .ToArray();

        var skipLong =
            (long)(request.Page - 1) *
            request.PageSize;

        IReadOnlyList<PublicVacancyListItemDto>
            pageItems =
                skipLong > int.MaxValue
                    ? Array.Empty<PublicVacancyListItemDto>()
                    : ordered
                        .Skip((int)skipLong)
                        .Take(request.PageSize)
                        .ToArray();

        return VacancySearchQueryResult.Success(
            new PublicVacancyPageDto(
                pageItems,
                request.Page,
                request.PageSize,
                totalCount));
    }
}