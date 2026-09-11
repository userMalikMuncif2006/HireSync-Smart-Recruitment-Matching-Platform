namespace HireSync.Application.DTOs.Matching;

public enum JobMatchFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    JobSeekerNotReady = 2,
    VacancyUnavailable = 3
}

public sealed record JobMatchQueryResult(
    bool Succeeded,
    MatchResultDto? Match,
    JobMatchFailureReason FailureReason)
{
    public static JobMatchQueryResult Success(
        MatchResultDto match)
    {
        ArgumentNullException.ThrowIfNull(match);

        return new JobMatchQueryResult(
            true,
            match,
            JobMatchFailureReason.None);
    }

    public static JobMatchQueryResult Failure(
        JobMatchFailureReason reason)
    {
        if (reason == JobMatchFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new JobMatchQueryResult(
            false,
            null,
            reason);
    }
}
