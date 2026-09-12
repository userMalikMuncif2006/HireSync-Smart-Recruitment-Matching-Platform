using HireSync.Domain.Enums;

namespace HireSync.Domain.Entities;

public sealed class Notification
{
    public const int TitleMaxLength = 120;
    public const int MessageMaxLength = 500;

    public Guid Id { get; private set; }

    public Guid RecipientUserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public Guid JobApplicationId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ReadAtUtc { get; private set; }

    private Notification()
    {
    }

    public Notification(
        Guid id,
        Guid recipientUserId,
        NotificationType type,
        string title,
        string message,
        Guid jobApplicationId,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Notification ID cannot be empty.",
                nameof(id));
        }

        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Recipient user ID cannot be empty.",
                nameof(recipientUserId));
        }

        if (!Enum.IsDefined(typeof(NotificationType), type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Notification type is invalid.");
        }

        if (type != NotificationType.ApplicationStatusChanged)
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Only ApplicationStatusChanged notifications are supported.");
        }

        if (string.IsNullOrWhiteSpace(title)
            || title.Trim().Length > TitleMaxLength)
        {
            throw new ArgumentException(
                $"Notification title is required and must not exceed {TitleMaxLength} characters.",
                nameof(title));
        }

        if (string.IsNullOrWhiteSpace(message)
            || message.Trim().Length > MessageMaxLength)
        {
            throw new ArgumentException(
                $"Notification message is required and must not exceed {MessageMaxLength} characters.",
                nameof(message));
        }

        if (jobApplicationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Job application ID cannot be empty.",
                nameof(jobApplicationId));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Timestamp must be UTC.",
                nameof(createdAtUtc));
        }

        Id = id;
        RecipientUserId = recipientUserId;
        Type = type;
        Title = title.Trim();
        Message = message.Trim();
        JobApplicationId = jobApplicationId;
        IsRead = false;
        CreatedAtUtc = createdAtUtc;
        ReadAtUtc = null;
    }
}
