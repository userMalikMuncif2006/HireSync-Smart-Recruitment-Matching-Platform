using HireSync.Application.DTOs.Auth;

namespace HireSync.Application.Interfaces.Identity;

public interface IEmployerEmailVerificationService
{
    Task<EmployerEmailVerificationResult> VerifyAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default);
}
