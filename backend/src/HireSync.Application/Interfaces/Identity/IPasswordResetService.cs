using HireSync.Application.DTOs.Auth;

namespace HireSync.Application.Interfaces.Identity;

public interface IPasswordResetService
{
    Task<bool> RequestAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<PasswordResetCompleteResult> CompleteAsync(
        PasswordResetCompleteRequest request,
        CancellationToken cancellationToken = default);
}
