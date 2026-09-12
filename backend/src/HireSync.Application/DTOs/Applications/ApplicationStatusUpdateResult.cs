namespace HireSync.Application.DTOs.Applications;

public enum ApplicationStatusUpdateFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    NotFound = 2,
    InvalidTransition = 3,
    ConcurrencyConflict = 4,
    PersistenceFailed = 5
}

public sealed record ApplicationStatusUpdateResult(
    bool Succeeded,
    ApplicationStatusDto? Application,
    ApplicationStatusUpdateFailureReason FailureReason)
{
    public static ApplicationStatusUpdateResult Success(
        ApplicationStatusDto application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return new ApplicationStatusUpdateResult(
            true,
            application,
            ApplicationStatusUpdateFailureReason.None);
    }

    public static ApplicationStatusUpdateResult Failure(
        ApplicationStatusUpdateFailureReason reason)
    {
        if (reason ==
            ApplicationStatusUpdateFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new ApplicationStatusUpdateResult(
            false,
            null,
            reason);
    }
}
