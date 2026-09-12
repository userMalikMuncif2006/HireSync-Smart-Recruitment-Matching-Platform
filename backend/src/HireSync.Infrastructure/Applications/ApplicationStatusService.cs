using HireSync.Application.DTOs.Applications;
using HireSync.Application.Interfaces.Applications;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Applications;

public sealed class ApplicationStatusService
    : IApplicationStatusService
{
    private const string NotificationTitle =
        "Application status updated";

    private readonly HireSyncDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public ApplicationStatusService(
        HireSyncDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<ApplicationStatusUpdateResult>
        UpdateOwnApplicationStatusAsync(
            Guid employerUserId,
            Guid applicationId,
            UpdateApplicationStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (employerUserId == Guid.Empty ||
            applicationId == Guid.Empty ||
            request is null ||
            !ApplicationStatusUpdateRules.IsValid(request))
        {
            return ApplicationStatusUpdateResult.Failure(
                ApplicationStatusUpdateFailureReason.InvalidInput);
        }

        var employer =
            await _userManager.Users
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == employerUserId,
                    cancellationToken);

        if (!await IsAuthorizedEmployerAsync(
                employer,
                cancellationToken))
        {
            return ApplicationStatusUpdateResult.Failure(
                ApplicationStatusUpdateFailureReason.NotFound);
        }

        var owned =
            await (
                from jobApplication in
                    _dbContext.JobApplications

                join vacancy in
                    _dbContext.Vacancies
                    on jobApplication.VacancyId
                    equals vacancy.Id

                join employerProfile in
                    _dbContext.EmployerProfiles
                    on vacancy.EmployerProfileId
                    equals employerProfile.Id

                join jobSeekerProfile in
                    _dbContext.JobSeekerProfiles
                    on jobApplication.JobSeekerProfileId
                    equals jobSeekerProfile.Id

                where
                    jobApplication.Id == applicationId &&
                    employerProfile.UserId ==
                        employerUserId

                select new
                {
                    Application = jobApplication,
                    RecipientUserId =
                        jobSeekerProfile.UserId
                })
            .SingleOrDefaultAsync(
                cancellationToken);

        if (owned is null)
        {
            return ApplicationStatusUpdateResult.Failure(
                ApplicationStatusUpdateFailureReason.NotFound);
        }

        var application =
            owned.Application;

        bool changed;

        try
        {
            changed =
                application.ChangeStatus(
                    request.Status,
                    _clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return ApplicationStatusUpdateResult.Failure(
                ApplicationStatusUpdateFailureReason
                    .InvalidTransition);
        }
        catch (ArgumentOutOfRangeException)
        {
            return ApplicationStatusUpdateResult.Failure(
                ApplicationStatusUpdateFailureReason
                    .InvalidInput);
        }

        // Canonical same-state behavior:
        // safe success, no write, no notification,
        // and stale RowVersion must not matter.
        if (!changed)
        {
            return ApplicationStatusUpdateResult.Success(
                ToDto(application));
        }

        _dbContext.Entry(application)
            .Property(candidate =>
                candidate.RowVersion)
            .OriginalValue =
                request.RowVersion;

        var notification =
            new Notification(
                Guid.NewGuid(),
                owned.RecipientUserId,
                NotificationType.ApplicationStatusChanged,
                NotificationTitle,
                $"Your application status changed to {application.Status}.",
                application.Id,
                _clock.UtcNow);

        _dbContext.Notifications.Add(
            notification);

        try
        {
            // One SaveChanges call intentionally persists
            // both the status transition and notification.
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApplicationStatusUpdateResult.Failure(
                ApplicationStatusUpdateFailureReason
                    .ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return ApplicationStatusUpdateResult.Failure(
                ApplicationStatusUpdateFailureReason
                    .PersistenceFailed);
        }

        return ApplicationStatusUpdateResult.Success(
            ToDto(application));
    }

    private async Task<bool>
        IsAuthorizedEmployerAsync(
            ApplicationUser? employer,
            CancellationToken cancellationToken)
    {
        if (employer is null ||
            employer.AccountStatus != AccountStatus.Active ||
            employer.EmployerVerificationStatus !=
                EmployerVerificationStatus.Approved)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var roles =
            await _userManager.GetRolesAsync(
                employer);

        cancellationToken.ThrowIfCancellationRequested();

        return roles.Count == 1 &&
               string.Equals(
                   roles[0],
                   RoleNames.Employer,
                   StringComparison.Ordinal);
    }

    private static ApplicationStatusDto ToDto(
        JobApplication application)
    {
        return new ApplicationStatusDto(
            application.Id,
            application.Status,
            application.UpdatedAtUtc,
            application.RowVersion);
    }
}
