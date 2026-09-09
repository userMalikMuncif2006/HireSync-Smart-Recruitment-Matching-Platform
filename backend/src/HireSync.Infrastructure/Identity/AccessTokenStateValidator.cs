using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HireSync.Infrastructure.Identity;

public sealed class AccessTokenStateValidator
    : IAccessTokenStateValidator
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccessTokenStateValidator(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> IsValidAsync(
        Guid userId,
        string role,
        int tokenVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == Guid.Empty ||
            tokenVersion < 1 ||
            !RoleNames.All.Contains(role, StringComparer.Ordinal))
        {
            return false;
        }

        var user =
            await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return false;
        }

        if (user.AccountStatus != AccountStatus.Active)
        {
            return false;
        }

        if (user.TokenVersion != tokenVersion)
        {
            return false;
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Count != 1)
        {
            return false;
        }

        return string.Equals(
            roles[0],
            role,
            StringComparison.Ordinal);
    }
}
