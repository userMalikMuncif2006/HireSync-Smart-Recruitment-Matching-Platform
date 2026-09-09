using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HireSync.Infrastructure.Identity;

public sealed class AdministratorActivationCompleter
    : IAdministratorActivationCompleter
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public AdministratorActivationCompleter(
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<bool> MarkActivatedAsync(
        Guid administratorUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user =
            await _userManager.FindByIdAsync(
                administratorUserId.ToString());

        if (user is null)
        {
            return false;
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        if (roles.Count != 1 ||
            !string.Equals(
                roles[0],
                RoleNames.Administrator,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (user.AccountStatus != AccountStatus.Active)
        {
            return false;
        }

        if (user.EmailConfirmed)
        {
            return true;
        }

        user.EmailConfirmed = true;
        user.UpdatedAtUtc = _clock.UtcNow;

        var updateResult =
            await _userManager.UpdateAsync(user);

        return updateResult.Succeeded;
    }
}
