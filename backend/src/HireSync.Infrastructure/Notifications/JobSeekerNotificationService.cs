using HireSync.Application.DTOs.Notifications;
using HireSync.Application.Interfaces.Notifications;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Notifications;

public sealed class JobSeekerNotificationService
    : IJobSeekerNotificationService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly IClock _clock;

    public JobSeekerNotificationService(
        HireSyncDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<NotificationPageQueryResult>
        GetOwnNotificationsAsync(
            Guid jobSeekerUserId,
            NotificationListRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty ||
            request is null ||
            !NotificationListRules.IsValid(request))
        {
            return NotificationPageQueryResult.Failure(
                NotificationReadFailureReason.InvalidInput);
        }

        if (!await IsEligibleJobSeekerAsync(
                jobSeekerUserId,
                cancellationToken))
        {
            return NotificationPageQueryResult.Failure(
                NotificationReadFailureReason
                    .JobSeekerUnavailable);
        }

        var query =
            _dbContext.Notifications
                .AsNoTracking()
                .Where(
                    notification =>
                        notification.RecipientUserId ==
                            jobSeekerUserId &&
                        notification.Type ==
                            NotificationType
                                .ApplicationStatusChanged);

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var skipLong =
            (long)(request.Page - 1) *
            request.PageSize;

        if (skipLong > int.MaxValue)
        {
            return NotificationPageQueryResult.Success(
                new NotificationPageDto(
                    Array.Empty<NotificationDto>(),
                    request.Page,
                    request.PageSize,
                    totalCount));
        }

        var items =
            await query
                .OrderByDescending(
                    notification =>
                        notification.CreatedAtUtc)
                .ThenBy(
                    notification =>
                        notification.Id)
                .Skip((int)skipLong)
                .Take(request.PageSize)
                .Select(
                    notification =>
                        new NotificationDto(
                            notification.Id,
                            notification.Type,
                            notification.Title,
                            notification.Message,
                            notification.JobApplicationId,
                            notification.IsRead,
                            notification.CreatedAtUtc,
                            notification.ReadAtUtc))
                .ToListAsync(
                    cancellationToken);

        return NotificationPageQueryResult.Success(
            new NotificationPageDto(
                items,
                request.Page,
                request.PageSize,
                totalCount));
    }

    public async Task<NotificationMutationResult>
        MarkReadAsync(
            Guid jobSeekerUserId,
            Guid notificationId,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty ||
            notificationId == Guid.Empty)
        {
            return NotificationMutationResult.Failure(
                NotificationReadFailureReason.InvalidInput);
        }

        if (!await IsEligibleJobSeekerAsync(
                jobSeekerUserId,
                cancellationToken))
        {
            return NotificationMutationResult.Failure(
                NotificationReadFailureReason
                    .JobSeekerUnavailable);
        }

        var notification =
            await _dbContext.Notifications
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == notificationId &&
                        candidate.RecipientUserId ==
                            jobSeekerUserId &&
                        candidate.Type ==
                            NotificationType
                                .ApplicationStatusChanged,
                    cancellationToken);

        if (notification is null)
        {
            return NotificationMutationResult.Failure(
                NotificationReadFailureReason
                    .NotificationNotFound);
        }

        var changed =
            notification.MarkRead(
                _clock.UtcNow);

        if (changed)
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return NotificationMutationResult.Success(
            changed ? 1 : 0);
    }

    public async Task<NotificationMutationResult>
        MarkAllReadAsync(
            Guid jobSeekerUserId,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty)
        {
            return NotificationMutationResult.Failure(
                NotificationReadFailureReason.InvalidInput);
        }

        if (!await IsEligibleJobSeekerAsync(
                jobSeekerUserId,
                cancellationToken))
        {
            return NotificationMutationResult.Failure(
                NotificationReadFailureReason
                    .JobSeekerUnavailable);
        }

        var notifications =
            await _dbContext.Notifications
                .Where(
                    notification =>
                        notification.RecipientUserId ==
                            jobSeekerUserId &&
                        notification.Type ==
                            NotificationType
                                .ApplicationStatusChanged &&
                        !notification.IsRead)
                .ToListAsync(
                    cancellationToken);

        if (notifications.Count == 0)
        {
            return NotificationMutationResult.Success(
                0);
        }

        var readAtUtc =
            _clock.UtcNow;

        var changedCount = 0;

        foreach (var notification in notifications)
        {
            if (notification.MarkRead(
                    readAtUtc))
            {
                changedCount++;
            }
        }

        if (changedCount > 0)
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return NotificationMutationResult.Success(
            changedCount);
    }

    private async Task<bool>
        IsEligibleJobSeekerAsync(
            Guid jobSeekerUserId,
            CancellationToken cancellationToken)
    {
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
            return false;
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

        return roleNames.Count == 1 &&
               string.Equals(
                   roleNames[0],
                   RoleNames.JobSeeker,
                   StringComparison.Ordinal);
    }
}
