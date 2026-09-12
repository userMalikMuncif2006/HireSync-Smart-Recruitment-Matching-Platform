using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Applications;

public sealed record ApplicationCreatedDto(
    Guid Id,
    Guid VacancyId,
    ApplicationStatus Status,
    DateTime AppliedAtUtc,
    DateTime UpdatedAtUtc);