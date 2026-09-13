using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.ContactRequests;

public sealed record JobSeekerContactRequestDto(
    Guid Id,
    Guid JobApplicationId,
    Guid VacancyId,
    string VacancyTitle,
    string EmployerCompanyName,
    ContactRequestStatus Status,
    DateTime RequestedAtUtc,
    DateTime? RespondedAtUtc,
    byte[] RowVersion);
