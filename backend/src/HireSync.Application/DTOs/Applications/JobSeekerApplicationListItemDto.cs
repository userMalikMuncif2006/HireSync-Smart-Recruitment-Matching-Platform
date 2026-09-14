using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Applications;

public sealed record JobSeekerApplicationListItemDto(
    Guid ApplicationId,
    Guid VacancyId,
    string VacancyTitle,
    string CompanyName,
    string VacancyLocation,
    VacancyStatus VacancyStatus,
    ApplicationStatus Status,
    DateTime AppliedAtUtc,
    DateTime UpdatedAtUtc);
