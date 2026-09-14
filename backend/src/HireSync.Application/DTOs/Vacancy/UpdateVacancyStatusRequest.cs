using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Vacancy;

public sealed record UpdateVacancyStatusRequest(
    VacancyStatus Status,
    byte[] RowVersion);