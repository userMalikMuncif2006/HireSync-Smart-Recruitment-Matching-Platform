namespace HireSync.Application.Interfaces.Identity;

public interface IIdentityService
{
    Task<AuthenticatedIdentity?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
