using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Domain.Enums;

namespace HireSync.Application.Services;

public sealed class AuthService
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public AuthService(
        IIdentityService identityService,
        ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return LoginResult.Failure(
                LoginFailureReason.InvalidCredentials);
        }

        var identity =
            await _identityService.ValidateCredentialsAsync(
                request.Email.Trim(),
                request.Password,
                cancellationToken);

        if (identity is null)
        {
            return LoginResult.Failure(
                LoginFailureReason.InvalidCredentials);
        }

        if (identity.AccountStatus == AccountStatus.Suspended)
        {
            return LoginResult.Failure(
                LoginFailureReason.Suspended);
        }

        if (string.Equals(
                identity.Role,
                RoleNames.Administrator,
                StringComparison.Ordinal) &&
            !identity.EmailConfirmed)
        {
            return LoginResult.Failure(
                LoginFailureReason.AdministratorActivationRequired);
        }

        var token = _tokenService.CreateAccessToken(
            identity.UserId,
            identity.Email,
            identity.Role,
            identity.TokenVersion);

        return LoginResult.Success(
            new LoginResponse(
                token.Token,
                token.ExpiresAtUtc,
                identity.UserId,
                identity.Email,
                identity.Role));
    }
}
