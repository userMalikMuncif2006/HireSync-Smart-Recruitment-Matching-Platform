using HireSync.Application.DTOs.Auth;

namespace HireSync.Application.Interfaces.Identity;

public interface IEmployerEmailVerificationService
{
    Task<EmployerEmailVerificationRequestResult> RequestAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<EmployerEmailVerificationResult> VerifyAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default);
}
