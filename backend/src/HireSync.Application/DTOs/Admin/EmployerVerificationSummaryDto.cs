using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Admin;

public sealed record EmployerVerificationSummaryDto(
    Guid UserId,
    string Email,
    string DisplayName,
    EmployerVerificationStatus Status);
