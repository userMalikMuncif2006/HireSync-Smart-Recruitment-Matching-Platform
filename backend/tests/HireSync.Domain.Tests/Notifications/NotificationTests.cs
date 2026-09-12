using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests.Notifications;

public sealed class NotificationTests
{
    [Fact]
    public void Constructor_CreatesUnreadApplicationStatusChangedNotification()
    {
        var createdAt = Utc();

        var notification = Create(createdAt);

        Assert.Equal(
            NotificationType.ApplicationStatusChanged,
            notification.Type);

        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
        Assert.Equal(createdAt, notification.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_TrimsSafePlainText()
    {
        var notification = new Notification(
            NotificationId(),
            RecipientId(),
            NotificationType.ApplicationStatusChanged,
            "  Application status changed  ",
            "  Your application status has changed.  ",
            ApplicationId(),
            Utc());

        Assert.Equal(
            "Application status changed",
            notification.Title);

        Assert.Equal(
            "Your application status has changed.",
            notification.Message);
    }

    [Fact]
    public void EnumValue_IsCanonical()
    {
        Assert.Equal(
            1,
            (byte)NotificationType.ApplicationStatusChanged);
    }

    [Fact]
    public void Constructor_EmptyRecipient_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new Notification(
                NotificationId(),
                Guid.Empty,
                NotificationType.ApplicationStatusChanged,
                "Status changed",
                "Your application status changed.",
                ApplicationId(),
                Utc()));
    }

    [Fact]
    public void Constructor_EmptyApplication_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new Notification(
                NotificationId(),
                RecipientId(),
                NotificationType.ApplicationStatusChanged,
                "Status changed",
                "Your application status changed.",
                Guid.Empty,
                Utc()));
    }

    [Fact]
    public void Constructor_OverlongTitle_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new Notification(
                NotificationId(),
                RecipientId(),
                NotificationType.ApplicationStatusChanged,
                new string('A', Notification.TitleMaxLength + 1),
                "Your application status changed.",
                ApplicationId(),
                Utc()));
    }

    [Fact]
    public void Constructor_OverlongMessage_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new Notification(
                NotificationId(),
                RecipientId(),
                NotificationType.ApplicationStatusChanged,
                "Status changed",
                new string('A', Notification.MessageMaxLength + 1),
                ApplicationId(),
                Utc()));
    }

    [Fact]
    public void Constructor_NonUtcTimestamp_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => Create(
                DateTime.SpecifyKind(
                    DateTime.UtcNow,
                    DateTimeKind.Local)));
    }

    [Fact]
    public void Notification_HasNoContactDataFields()
    {
        var propertyNames = typeof(Notification)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Email", propertyNames);
        Assert.DoesNotContain("Phone", propertyNames);
        Assert.DoesNotContain("ContactMessage", propertyNames);
        Assert.DoesNotContain("Attachment", propertyNames);
    }

    private static Notification Create(DateTime createdAtUtc)
    {
        return new Notification(
            NotificationId(),
            RecipientId(),
            NotificationType.ApplicationStatusChanged,
            "Application status changed",
            "Your application status has changed.",
            ApplicationId(),
            createdAtUtc);
    }

    private static Guid NotificationId() =>
        Guid.Parse("40000000-0000-0000-0000-000000000001");

    private static Guid RecipientId() =>
        Guid.Parse("50000000-0000-0000-0000-000000000001");

    private static Guid ApplicationId() =>
        Guid.Parse("60000000-0000-0000-0000-000000000001");

    private static DateTime Utc() =>
        new(
            2026,
            9,
            12,
            12,
            0,
            0,
            DateTimeKind.Utc);
}
