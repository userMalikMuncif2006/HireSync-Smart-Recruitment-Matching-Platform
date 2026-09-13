using HireSync.Application.DTOs.Auth;

namespace HireSync.Application.Interfaces.Identity;

public interface IJobSeekerEmailVerificationService
{
    Task<JobSeekerEmailVerificationRequestResult> RequestAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<JobSeekerEmailVerificationResult> VerifyAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default);
}
