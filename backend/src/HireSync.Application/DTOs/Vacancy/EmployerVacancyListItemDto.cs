using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record EmployerVacancyListItemDto(
    Guid Id,
    string Title,
    string Location,
    VacancyStatus Status,
    DateTime PublishedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ClosedAtUtc,
    byte[] RowVersion);