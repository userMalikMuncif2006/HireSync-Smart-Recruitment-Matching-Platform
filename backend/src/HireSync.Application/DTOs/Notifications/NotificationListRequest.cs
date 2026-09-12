namespace HireSync.Application.DTOs.Notifications;

public sealed record NotificationListRequest(
    int Page = 1,
    int PageSize = 20);
