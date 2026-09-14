using HireSync.Application.DTOs.Applications;
using HireSync.Application.Interfaces.Applications;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Applications;

public sealed class JobSeekerApplicationTrackingService
    : IJobSeekerApplicationTrackingService
{
    private readonly HireSyncDbContext _dbContext;

    public JobSeekerApplicationTrackingService(
        HireSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JobSeekerApplicationTrackingResult>
        GetOwnApplicationsAsync(
            Guid jobSeekerUserId,
            JobSeekerApplicationListRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty ||
            request is null ||
            !JobSeekerApplicationListRules.IsValid(request))
        {
            return JobSeekerApplicationTrackingResult.Failure(
                JobSeekerApplicationTrackingFailureReason.InvalidInput);
        }

        var user =
            await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == jobSeekerUserId,
                    cancellationToken);

        if (user is null ||
            user.AccountStatus != AccountStatus.Active)
        {
            return JobSeekerApplicationTrackingResult.Failure(
                JobSeekerApplicationTrackingFailureReason
                    .JobSeekerUnavailable);
        }

        var roleNames =
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

        if (roleNames.Count != 1 ||
            !string.Equals(
                roleNames[0],
                RoleNames.JobSeeker,
                StringComparison.Ordinal))
        {
            return JobSeekerApplicationTrackingResult.Failure(
                JobSeekerApplicationTrackingFailureReason
                    .JobSeekerUnavailable);
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
            return JobSeekerApplicationTrackingResult.Success(
                new JobSeekerApplicationPageDto(
                    Array.Empty<
                        JobSeekerApplicationListItemDto>(),
                    request.Page,
                    request.PageSize,
                    0));
        }

        var query =
            from application in
                _dbContext.JobApplications.AsNoTracking()

            join vacancy in
                _dbContext.Vacancies.AsNoTracking()
                on application.VacancyId
                equals vacancy.Id

            join employerProfile in
                _dbContext.EmployerProfiles.AsNoTracking()
                on vacancy.EmployerProfileId
                equals employerProfile.Id

            where
                application.JobSeekerProfileId ==
                    profile.Id

            select new
            {
                Application = application,
                Vacancy = vacancy,
                EmployerProfile = employerProfile
            };

        if (request.Status.HasValue)
        {
            query =
                query.Where(
                    candidate =>
                        candidate.Application.Status ==
                            request.Status.Value);
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var skipLong =
            (long)(request.Page - 1) *
            request.PageSize;

        if (skipLong > int.MaxValue)
        {
            return JobSeekerApplicationTrackingResult.Success(
                new JobSeekerApplicationPageDto(
                    Array.Empty<
                        JobSeekerApplicationListItemDto>(),
                    request.Page,
                    request.PageSize,
                    totalCount));
        }

        var items =
            await query
                .OrderByDescending(
                    candidate =>
                        candidate.Application.AppliedAtUtc)
                .ThenBy(
                    candidate =>
                        candidate.Application.Id)
                .Skip((int)skipLong)
                .Take(request.PageSize)
                .Select(
                    candidate =>
                        new JobSeekerApplicationListItemDto(
                            candidate.Application.Id,
                            candidate.Vacancy.Id,
                            candidate.Vacancy.Title,
                            candidate.EmployerProfile.CompanyName,
                            candidate.Vacancy.Location,
                            candidate.Vacancy.Status,
                            candidate.Application.Status,
                            candidate.Application.AppliedAtUtc,
                            candidate.Application.UpdatedAtUtc))
                .ToListAsync(
                    cancellationToken);

        return JobSeekerApplicationTrackingResult.Success(
            new JobSeekerApplicationPageDto(
                items,
                request.Page,
                request.PageSize,
                totalCount));
    }
}
