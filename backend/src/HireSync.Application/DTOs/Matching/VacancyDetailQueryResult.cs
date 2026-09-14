using HireSync.Application.DTOs.Vacancy;

namespace HireSync.Application.DTOs.Matching;

public enum VacancyDetailFailureReason : byte
{
    None = 0,
    InvalidInput = 1,
    VacancyUnavailable = 2
}

public sealed record VacancyDetailQueryResult(
    bool Succeeded,
    PublicVacancyDetailDto? Detail,
    VacancyDetailFailureReason FailureReason)
{
    public static VacancyDetailQueryResult Success(
        PublicVacancyDetailDto detail)
    {
        ArgumentNullException.ThrowIfNull(
            detail);

        return new VacancyDetailQueryResult(
            true,
            detail,
            VacancyDetailFailureReason.None);
    }

    public static VacancyDetailQueryResult Failure(
        VacancyDetailFailureReason reason)
    {
        if (reason ==
            VacancyDetailFailureReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        return new VacancyDetailQueryResult(
            false,
            null,
            reason);
    }
}