using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record EmployerVacancyListRequest(
    VacancyStatus? Status = null,
    int Page = 1,
    int PageSize = 20);