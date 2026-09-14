namespace HireSync.Application.DTOs.Vacancy;

public sealed record VacancyCreateResult(
    VacancyDto? Vacancy,
    VacancyCreateFailureReason FailureReason)
{
    public bool Succeeded =>
        FailureReason == VacancyCreateFailureReason.None &&
        Vacancy is not null;

    public static VacancyCreateResult Success(
        VacancyDto vacancy)
    {
        return new VacancyCreateResult(
            vacancy,
            VacancyCreateFailureReason.None);
    }

    public static VacancyCreateResult Failure(
        VacancyCreateFailureReason failureReason)
    {
        return new VacancyCreateResult(
            null,
            failureReason);
    }
}