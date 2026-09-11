namespace HireSync.Application.DTOs.Vacancy;

public sealed record EmployerVacancyPageDto(
    IReadOnlyList<EmployerVacancyListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);