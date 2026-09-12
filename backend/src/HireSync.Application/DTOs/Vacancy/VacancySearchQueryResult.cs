namespace HireSync.Application.DTOs.Vacancy;

public enum VacancySearchFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    JobSeekerUnavailable = 2,
    ProfileNotReady = 3,
    InvalidMatchingData = 4
}

public sealed record VacancySearchQueryResult(
    bool Succeeded,
    PublicVacancyPageDto? Page,
    VacancySearchFailureReason FailureReason)
{
    public static VacancySearchQueryResult Success(
        PublicVacancyPageDto page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new VacancySearchQueryResult(
            true,
            page,
            VacancySearchFailureReason.None);
    }

    public static VacancySearchQueryResult Failure(
        VacancySearchFailureReason reason)
    {
        if (reason == VacancySearchFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new VacancySearchQueryResult(
            false,
            null,
            reason);
    }
}