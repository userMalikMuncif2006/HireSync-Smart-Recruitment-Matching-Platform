namespace HireSync.Application.DTOs.Applications;

public enum ApplicationCreateFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    JobSeekerUnavailable = 2,
    ProfileNotReady = 3,
    CurrentCvRequired = 4,
    VacancyUnavailable = 5,
    VacancyClosed = 6,
    AlreadyApplied = 7,
    PersistenceFailed = 8
}

public sealed record ApplicationCreateResult(
    bool Succeeded,
    ApplicationCreatedDto? Application,
    ApplicationCreateFailureReason FailureReason)
{
    public static ApplicationCreateResult Success(
        ApplicationCreatedDto application)
    {
        ArgumentNullException.ThrowIfNull(
            application);

        return new ApplicationCreateResult(
            true,
            application,
            ApplicationCreateFailureReason.None);
    }

    public static ApplicationCreateResult Failure(
        ApplicationCreateFailureReason reason)
    {
        if (reason ==
            ApplicationCreateFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new ApplicationCreateResult(
            false,
            null,
            reason);
    }
}