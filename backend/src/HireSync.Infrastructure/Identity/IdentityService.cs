using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HireSync.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<AuthenticatedIdentity?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null)
        {
            return null;
        }

        var passwordValid =
            await _userManager.CheckPasswordAsync(user, password);

        if (!passwordValid)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Count != 1)
        {
            return null;
        }

        var role = roles[0];

        if (!RoleNames.All.Contains(role, StringComparer.Ordinal))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return null;
        }

        return new AuthenticatedIdentity(
            user.Id,
            user.Email,
            role,
            user.TokenVersion,
            user.AccountStatus);
    }

    public async Task<IdentityUserCreationResult> CreateUserAsync(
        string email,
        string password,
        string displayName,
        string role,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(displayName) ||
            !RoleNames.All.Contains(role, StringComparer.Ordinal))
        {
            return IdentityUserCreationResult.Failure(
                IdentityUserCreationFailure.ValidationFailed);
        }

        var normalizedEmail = email.Trim();

        var existingUser =
            await _userManager.FindByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            return IdentityUserCreationResult.Failure(
                IdentityUserCreationFailure.EmailAlreadyExists);
        }

        var now = _clock.UtcNow;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = displayName.Trim(),
            AccountStatus = AccountStatus.Active,
            EmployerVerificationStatus =
                role == RoleNames.Employer
                    ? HireSync.Domain.Enums.EmployerVerificationStatus.Pending
                    : null,
            TokenVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var createResult =
            await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return IdentityUserCreationResult.Failure(
                IdentityUserCreationFailure.ValidationFailed);
        }

        var roleResult =
            await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return IdentityUserCreationResult.Failure(
                IdentityUserCreationFailure.ValidationFailed);
        }

        return IdentityUserCreationResult.Success(
            user.Id,
            user.Email!,
            user.DisplayName);
    }
}
