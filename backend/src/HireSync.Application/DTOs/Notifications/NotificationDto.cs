using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    Guid JobApplicationId,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
