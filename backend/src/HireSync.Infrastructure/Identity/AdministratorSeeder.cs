using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HireSync.Infrastructure.Identity;

public sealed class AdministratorSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdministratorSeedSettings _settings;
    private readonly IClock _clock;

    public AdministratorSeeder(
        UserManager<ApplicationUser> userManager,
        AdministratorSeedSettings settings,
        IClock clock)
    {
        _userManager = userManager;
        _settings = settings;
        _clock = clock;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_settings.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.Email) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.DisplayName))
        {
            throw new InvalidOperationException(
                "Administrator seed configuration is incomplete.");
        }

        var email = _settings.Email.Trim();

        var existing =
            await _userManager.FindByEmailAsync(email);

        if (existing is not null)
        {
            var existingRoles =
                await _userManager.GetRolesAsync(existing);

            if (existingRoles.Count == 1 &&
                string.Equals(
                    existingRoles[0],
                    RoleNames.Administrator,
                    StringComparison.Ordinal))
            {
                return;
            }

            throw new InvalidOperationException(
                "Administrator seed email already belongs to a non-Administrator account.");
        }

        var now = _clock.UtcNow;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = _settings.DisplayName.Trim(),
            AccountStatus = AccountStatus.Active,
            EmployerVerificationStatus = null,
            EmailConfirmed = false,
            TokenVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var createResult =
            await _userManager.CreateAsync(
                user,
                _settings.Password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Administrator account could not be securely seeded.");
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                RoleNames.Administrator);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            throw new InvalidOperationException(
                "Administrator role assignment failed during seeding.");
        }
    }
}


