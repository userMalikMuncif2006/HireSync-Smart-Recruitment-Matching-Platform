namespace HireSync.Application.DTOs.Vacancy;

public sealed record PublicVacancyPageDto(
    IReadOnlyList<PublicVacancyListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);