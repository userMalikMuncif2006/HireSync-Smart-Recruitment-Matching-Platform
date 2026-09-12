using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Vacancy;
using HireSync.Application.Rules;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Search;

public sealed class VacancySearchService : IVacancySearchService
{
    private readonly HireSyncDbContext _dbContext;

    public VacancySearchService(
        HireSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PublicVacancyPageDto> SearchOpenVacanciesAsync(
        SearchVacanciesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        if (!VacancySearchRules.IsValid(request) ||
            !VacancySearchRules.IsBasicSearchSortSupported(
                request.Sort))
        {
            throw new ArgumentException(
                "The vacancy search request is invalid or uses an unsupported sort.",
                nameof(request));
        }

        var query =
            from vacancy in _dbContext.Vacancies.AsNoTracking()
            join profile in _dbContext.EmployerProfiles.AsNoTracking()
                on vacancy.EmployerProfileId equals profile.Id
            join user in _dbContext.Users.AsNoTracking()
                on profile.UserId equals user.Id
            where
                vacancy.Status == VacancyStatus.Open &&
                user.AccountStatus == AccountStatus.Active &&
                user.EmployerVerificationStatus ==
                    EmployerVerificationStatus.Approved &&

                profile.CompanyName.Trim() != string.Empty &&
                profile.Description.Trim() != string.Empty &&
                profile.Location.Trim() != string.Empty &&
                profile.ContactPersonName.Trim() != string.Empty &&
                profile.ContactPersonDesignation.Trim() != string.Empty &&
                profile.BusinessRegistrationNumber.Trim() != string.Empty &&
                profile.MobileNumber.Trim() != string.Empty

            select new
            {
                Vacancy = vacancy,
                Profile = profile
            };

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var normalizedQuery =
                request.Q.Trim().ToUpperInvariant();

            query = query.Where(candidate =>
                candidate.Vacancy.Title
                    .ToUpper()
                    .Contains(normalizedQuery) ||

                candidate.Vacancy.Description
                    .ToUpper()
                    .Contains(normalizedQuery) ||

                candidate.Profile.CompanyName
                    .ToUpper()
                    .Contains(normalizedQuery));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            string normalizedLocation;

            try
            {
                normalizedLocation =
                    LocationNormalizer.Normalize(
                        request.Location);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException(
                    "The vacancy search location is invalid.",
                    nameof(request),
                    exception);
            }

            query = query.Where(candidate =>
                candidate.Vacancy.NormalizedLocation ==
                    normalizedLocation);
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var skipLong =
            (long)(request.Page - 1) *
            request.PageSize;

        if (skipLong > int.MaxValue)
        {
            return new PublicVacancyPageDto(
                Array.Empty<PublicVacancyListItemDto>(),
                request.Page,
                request.PageSize,
                totalCount);
        }

        var items =
            await query
                .OrderByDescending(candidate =>
                    candidate.Vacancy.PublishedAtUtc)
                .ThenBy(candidate =>
                    candidate.Vacancy.Id)
                .Skip((int)skipLong)
                .Take(request.PageSize)
                .Select(candidate =>
                    new PublicVacancyListItemDto(
                        candidate.Vacancy.Id,
                        candidate.Vacancy.Title,
                        candidate.Profile.CompanyName,
                        candidate.Vacancy.Location,
                        candidate.Vacancy.MinimumExperienceMonths,
                        candidate.Vacancy.RequiredEducationLevel,
                        candidate.Vacancy.PublishedAtUtc,
                        null))
                .ToListAsync(
                    cancellationToken);

        return new PublicVacancyPageDto(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }
}