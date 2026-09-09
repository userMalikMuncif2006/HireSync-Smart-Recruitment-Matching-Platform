using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Employer;

public sealed record EmployerProfileDto(
    Guid Id,
    string CompanyName,
    string Description,
    string Location,
    string ContactPersonName,
    string ContactPersonDesignation,
    string BusinessRegistrationNumber,
    string MobileNumber,
    string? CompanyWebsite,
    string BusinessEmail,
    EmployerVerificationStatus EmployerVerificationStatus,
    bool IsProfileComplete,
    bool IsVacancyReady);