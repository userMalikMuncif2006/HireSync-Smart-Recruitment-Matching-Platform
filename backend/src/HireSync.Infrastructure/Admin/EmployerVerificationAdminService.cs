using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Admin;

public sealed class EmployerVerificationAdminService
    : IEmployerVerificationAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public EmployerVerificationAdminService(
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<IReadOnlyList<EmployerVerificationSummaryDto>>
        GetPendingEmployersAsync(
            CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .Where(user =>
                user.EmployerVerificationStatus ==
                EmployerVerificationStatus.Pending)
            .OrderBy(user => user.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var employers =
            new List<EmployerVerificationSummaryDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Count != 1 ||
                !string.Equals(
                    roles[0],
                    RoleNames.Employer,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                continue;
            }

            employers.Add(
                new EmployerVerificationSummaryDto(
                    user.Id,
                    user.Email,
                    user.DisplayName,
                    EmployerVerificationStatus.Pending));
        }

        return employers;
    }

    public Task<EmployerVerificationUpdateResult> ApproveAsync(
        Guid employerUserId,
        CancellationToken cancellationToken = default)
    {
        return UpdateStatusAsync(
            employerUserId,
            EmployerVerificationStatus.Approved,
            cancellationToken);
    }

    public Task<EmployerVerificationUpdateResult> RejectAsync(
        Guid employerUserId,
        CancellationToken cancellationToken = default)
    {
        return UpdateStatusAsync(
            employerUserId,
            EmployerVerificationStatus.Rejected,
            cancellationToken);
    }

    private async Task<EmployerVerificationUpdateResult>
        UpdateStatusAsync(
            Guid employerUserId,
            EmployerVerificationStatus targetStatus,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == employerUserId,
                cancellationToken);

        if (user is null)
        {
            return EmployerVerificationUpdateResult.Failure(
                EmployerVerificationUpdateFailureReason.NotFound);
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Count != 1 ||
            !string.Equals(
                roles[0],
                RoleNames.Employer,
                StringComparison.Ordinal))
        {
            return EmployerVerificationUpdateResult.Failure(
                EmployerVerificationUpdateFailureReason.NotFound);
        }

        if (!user.EmployerVerificationStatus.HasValue ||
            !EmployerVerificationPolicy.CanTransition(
                user.EmployerVerificationStatus.Value,
                targetStatus))
        {
            return EmployerVerificationUpdateResult.Failure(
                EmployerVerificationUpdateFailureReason.InvalidTransition);
        }

        user.EmployerVerificationStatus = targetStatus;
        user.UpdatedAtUtc = _clock.UtcNow;

        var updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return EmployerVerificationUpdateResult.Failure(
                EmployerVerificationUpdateFailureReason.PersistenceFailed);
        }

        return EmployerVerificationUpdateResult.Success(
            new EmployerVerificationSummaryDto(
                user.Id,
                user.Email!,
                user.DisplayName,
                targetStatus));
    }
}
