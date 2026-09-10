using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record VacancyStatusDto(
    Guid Id,
    VacancyStatus Status,
    DateTime? ClosedAtUtc,
    byte[] RowVersion);