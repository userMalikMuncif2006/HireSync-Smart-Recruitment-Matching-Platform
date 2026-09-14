using HireSync.Application.DTOs.Employer;

namespace HireSync.Application.Interfaces.Employer;

public interface IEmployerProfileService
{
    Task<EmployerProfileDto?> GetOwnProfileAsync(
        Guid employerUserId,
        CancellationToken cancellationToken = default);

    Task<EmployerProfileUpdateResult> UpdateOwnProfileAsync(
        Guid employerUserId,
        UpdateEmployerProfileRequest request,
        CancellationToken cancellationToken = default);
}