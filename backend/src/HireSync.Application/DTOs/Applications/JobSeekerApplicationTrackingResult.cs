namespace HireSync.Application.DTOs.Applications;

public enum JobSeekerApplicationTrackingFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    JobSeekerUnavailable = 2
}

public sealed record JobSeekerApplicationTrackingResult(
    bool Succeeded,
    JobSeekerApplicationPageDto? Page,
    JobSeekerApplicationTrackingFailureReason FailureReason)
{
    public static JobSeekerApplicationTrackingResult Success(
        JobSeekerApplicationPageDto page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new JobSeekerApplicationTrackingResult(
            true,
            page,
            JobSeekerApplicationTrackingFailureReason.None);
    }

    public static JobSeekerApplicationTrackingResult Failure(
        JobSeekerApplicationTrackingFailureReason reason)
    {
        if (reason ==
            JobSeekerApplicationTrackingFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new JobSeekerApplicationTrackingResult(
            false,
            null,
            reason);
    }
}
