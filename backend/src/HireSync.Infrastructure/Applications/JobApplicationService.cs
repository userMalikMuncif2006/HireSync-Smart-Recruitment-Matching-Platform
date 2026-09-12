using System.Data;
using HireSync.Application.DTOs.Applications;
using HireSync.Application.Interfaces.Applications;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Applications;

public sealed class JobApplicationService
    : IJobApplicationService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public JobApplicationService(
        HireSyncDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<ApplicationCreateResult> CreateAsync(
        Guid jobSeekerUserId,
        Guid vacancyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty ||
            vacancyId == Guid.Empty)
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason.InvalidInput);
        }

        var jobSeeker =
            await _userManager.Users
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == jobSeekerUserId,
                    cancellationToken);

        if (!await HasExactRoleAsync(
                jobSeeker,
                RoleNames.JobSeeker,
                cancellationToken) ||
            jobSeeker!.AccountStatus !=
                AccountStatus.Active)
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason
                    .JobSeekerUnavailable);
        }

        var profile =
            await _dbContext.JobSeekerProfiles
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.UserId ==
                        jobSeekerUserId,
                    cancellationToken);

        if (profile is null)
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason
                    .ProfileNotReady);
        }

        var alreadyApplied =
            await _dbContext.JobApplications
                .AsNoTracking()
                .AnyAsync(
                    application =>
                        application.VacancyId ==
                            vacancyId &&
                        application.JobSeekerProfileId ==
                            profile.Id,
                    cancellationToken);

        if (alreadyApplied)
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason
                    .AlreadyApplied);
        }

        var hasSkill =
            await _dbContext.JobSeekerSkills
                .AsNoTracking()
                .AnyAsync(
                    candidate =>
                        candidate.JobSeekerProfileId ==
                        profile.Id,
                    cancellationToken);

        if (!profile.IsMatchReady(
                hasSkill))
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason
                    .ProfileNotReady);
        }

        var hasCurrentCv =
            await _dbContext.CvDocuments
                .AsNoTracking()
                .AnyAsync(
                    document =>
                        document.JobSeekerProfileId ==
                        profile.Id,
                    cancellationToken);

        if (!hasCurrentCv)
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason
                    .CurrentCvRequired);
        }

        await using var transaction =
            _dbContext.Database.IsRelational()
                ? await _dbContext.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken)
                : null;

        var vacancyContext =
            await (
                from vacancy in
                    _dbContext.Vacancies

                join employerProfile in
                    _dbContext.EmployerProfiles
                    on vacancy.EmployerProfileId
                    equals employerProfile.Id

                join employerUser in
                    _userManager.Users
                    on employerProfile.UserId
                    equals employerUser.Id

                where
                    vacancy.Id == vacancyId

                select new
                {
                    Vacancy = vacancy,
                    EmployerProfile = employerProfile,
                    EmployerUser = employerUser
                })
            .SingleOrDefaultAsync(
                cancellationToken);

        if (vacancyContext is null)
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason
                    .VacancyUnavailable);
        }

        if (vacancyContext.Vacancy.Status !=
            VacancyStatus.Open)
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason
                    .VacancyClosed);
        }

        if (vacancyContext.EmployerUser.AccountStatus !=
                AccountStatus.Active ||
            vacancyContext.EmployerUser
                .EmployerVerificationStatus !=
                EmployerVerificationStatus.Approved ||
            !vacancyContext.EmployerProfile
                .IsProfileComplete ||
            !await HasExactRoleAsync(
                vacancyContext.EmployerUser,
                RoleNames.Employer,
                cancellationToken))
        {
            return ApplicationCreateResult.Failure(
                ApplicationCreateFailureReason
                    .VacancyUnavailable);
        }

        var now =
            _clock.UtcNow;

        var application =
            new JobApplication(
                Guid.NewGuid(),
                vacancyId,
                profile.Id,
                now);

        _dbContext.JobApplications.Add(
            application);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(
                    cancellationToken);
            }
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);
            }

            _dbContext.Entry(application)
                .State =
                    EntityState.Detached;

            var duplicateExists =
                await _dbContext.JobApplications
                    .AsNoTracking()
                    .AnyAsync(
                        candidate =>
                            candidate.VacancyId ==
                                vacancyId &&
                            candidate.JobSeekerProfileId ==
                                profile.Id &&
                            candidate.Id !=
                                application.Id,
                        cancellationToken);

            return ApplicationCreateResult.Failure(
                duplicateExists
                    ? ApplicationCreateFailureReason
                        .AlreadyApplied
                    : ApplicationCreateFailureReason
                        .PersistenceFailed);
        }

        return ApplicationCreateResult.Success(
            new ApplicationCreatedDto(
                application.Id,
                application.VacancyId,
                application.Status,
                application.AppliedAtUtc,
                application.UpdatedAtUtc));
    }

    private async Task<bool> HasExactRoleAsync(
        ApplicationUser? user,
        string requiredRole,
        CancellationToken cancellationToken)
    {
        if (user is null)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var roles =
            await _userManager.GetRolesAsync(
                user);

        cancellationToken.ThrowIfCancellationRequested();

        return roles.Count == 1 &&
               string.Equals(
                   roles[0],
                   requiredRole,
                   StringComparison.Ordinal);
    }
}