namespace HireSync.Application.DTOs.EmployerApplications;

public enum RankedApplicantQueryFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    VacancyNotFound = 2,
    InvalidMatchingData = 3
}

public sealed record RankedApplicantQueryResult(
    bool Succeeded,
    RankedApplicantPageDto? Page,
    RankedApplicantQueryFailureReason FailureReason)
{
    public static RankedApplicantQueryResult Success(
        RankedApplicantPageDto page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new RankedApplicantQueryResult(
            true,
            page,
            RankedApplicantQueryFailureReason.None);
    }

    public static RankedApplicantQueryResult Failure(
        RankedApplicantQueryFailureReason reason)
    {
        if (reason ==
            RankedApplicantQueryFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new RankedApplicantQueryResult(
            false,
            null,
            reason);
    }
}