using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Identity;

namespace HireSync.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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
}
