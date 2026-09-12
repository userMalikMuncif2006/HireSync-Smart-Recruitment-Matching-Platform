namespace HireSync.Application.DTOs.Notifications;

public enum NotificationReadFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    JobSeekerUnavailable = 2,
    NotificationNotFound = 3
}

public sealed record NotificationPageQueryResult(
    bool Succeeded,
    NotificationPageDto? Page,
    NotificationReadFailureReason FailureReason)
{
    public static NotificationPageQueryResult Success(
        NotificationPageDto page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new NotificationPageQueryResult(
            true,
            page,
            NotificationReadFailureReason.None);
    }

    public static NotificationPageQueryResult Failure(
        NotificationReadFailureReason reason)
    {
        if (reason == NotificationReadFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new NotificationPageQueryResult(
            false,
            null,
            reason);
    }
}

public sealed record NotificationMutationResult(
    bool Succeeded,
    int ChangedCount,
    NotificationReadFailureReason FailureReason)
{
    public static NotificationMutationResult Success(
        int changedCount)
    {
        if (changedCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(changedCount));
        }

        return new NotificationMutationResult(
            true,
            changedCount,
            NotificationReadFailureReason.None);
    }

    public static NotificationMutationResult Failure(
        NotificationReadFailureReason reason)
    {
        if (reason == NotificationReadFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new NotificationMutationResult(
            false,
            0,
            reason);
    }
}
