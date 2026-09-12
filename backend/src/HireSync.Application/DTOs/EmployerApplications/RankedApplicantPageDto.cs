namespace HireSync.Application.DTOs.EmployerApplications;

public sealed record RankedApplicantPageDto(
    Guid VacancyId,
    string VacancyTitle,
    IReadOnlyList<RankedApplicantDto> Items,
    int Page,
    int PageSize,
    int TotalCount);