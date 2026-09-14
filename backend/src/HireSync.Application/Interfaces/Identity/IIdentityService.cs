namespace HireSync.Application.Interfaces.Identity;

public interface IIdentityService
{
    Task<AuthenticatedIdentity?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<IdentityUserCreationResult> CreateUserAsync(
        string email,
        string password,
        string displayName,
        string role,
        CancellationToken cancellationToken = default);
}
