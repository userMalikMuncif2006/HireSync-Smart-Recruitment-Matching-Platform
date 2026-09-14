namespace HireSync.Application.DTOs.Vacancy;

public sealed record VacancyStatusUpdateResult(
    VacancyStatusDto? Vacancy,
    VacancyStatusUpdateFailureReason FailureReason)
{
    public bool Succeeded =>
        FailureReason == VacancyStatusUpdateFailureReason.None &&
        Vacancy is not null;

    public static VacancyStatusUpdateResult Success(
        VacancyStatusDto vacancy)
    {
        return new VacancyStatusUpdateResult(
            vacancy,
            VacancyStatusUpdateFailureReason.None);
    }

    public static VacancyStatusUpdateResult Failure(
        VacancyStatusUpdateFailureReason failureReason)
    {
        return new VacancyStatusUpdateResult(
            null,
            failureReason);
    }
}