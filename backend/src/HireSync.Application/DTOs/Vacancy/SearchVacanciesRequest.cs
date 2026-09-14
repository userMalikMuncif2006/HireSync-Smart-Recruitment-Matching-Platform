namespace HireSync.Application.DTOs.Vacancy;

public sealed record SearchVacanciesRequest(
    string? Q = null,
    string? Location = null,
    VacancySearchSort Sort = VacancySearchSort.Newest,
    int Page = 1,
    int PageSize = 20);