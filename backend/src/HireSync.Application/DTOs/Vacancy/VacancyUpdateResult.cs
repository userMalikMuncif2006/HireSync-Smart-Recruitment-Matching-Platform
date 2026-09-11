namespace HireSync.Application.DTOs.Vacancy;

public sealed record VacancyUpdateResult(
    VacancyDto? Vacancy,
    VacancyUpdateFailureReason FailureReason)
{
    public bool Succeeded =>
        FailureReason == VacancyUpdateFailureReason.None &&
        Vacancy is not null;

    public static VacancyUpdateResult Success(
        VacancyDto vacancy)
    {
        return new VacancyUpdateResult(
            vacancy,
            VacancyUpdateFailureReason.None);
    }

    public static VacancyUpdateResult Failure(
        VacancyUpdateFailureReason failureReason)
    {
        return new VacancyUpdateResult(
            null,
            failureReason);
    }
}