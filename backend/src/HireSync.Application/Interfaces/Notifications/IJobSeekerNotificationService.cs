using HireSync.Application.DTOs.Notifications;

namespace HireSync.Application.Interfaces.Notifications;

public interface IJobSeekerNotificationService
{
    Task<NotificationPageQueryResult>
        GetOwnNotificationsAsync(
            Guid jobSeekerUserId,
            NotificationListRequest request,
            CancellationToken cancellationToken = default);

    Task<NotificationMutationResult>
        MarkReadAsync(
            Guid jobSeekerUserId,
            Guid notificationId,
            CancellationToken cancellationToken = default);

    Task<NotificationMutationResult>
        MarkAllReadAsync(
            Guid jobSeekerUserId,
            CancellationToken cancellationToken = default);
}
