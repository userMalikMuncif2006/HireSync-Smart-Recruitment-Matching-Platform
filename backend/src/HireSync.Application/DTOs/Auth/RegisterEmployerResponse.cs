using HireSync.Domain.Enums;

namespace HireSync.Application.DTOs.Auth;

public sealed record RegisterEmployerResponse(
    Guid UserId,
    Guid EmployerProfileId,
    string Email,
    string Role,
    EmployerVerificationStatus EmployerVerificationStatus);
