using HireSync.Application.DTOs.Auth;

namespace HireSync.Application.Interfaces.Identity;

public interface IEmployerRegistrationProvisioner
{
    Task<EmployerRegistrationResult> ProvisionAsync(
        RegisterEmployerRequest request,
        CancellationToken cancellationToken = default);
}
