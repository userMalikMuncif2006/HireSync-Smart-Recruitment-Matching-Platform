using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Admin;

public sealed class AdminAccountStatusService
    : IAdminAccountStatusService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly IClock _clock;

    public AdminAccountStatusService(
        HireSyncDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<AdminAccountStatusUpdateResult>
        UpdateStatusAsync(
            Guid administratorUserId,
            Guid targetUserId,
            UpdateAdminAccountStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (administratorUserId == Guid.Empty ||
            targetUserId == Guid.Empty ||
            request is null ||
            request.RowVersion is null ||
            request.RowVersion.Length == 0 ||
            !Enum.IsDefined(
                typeof(AccountStatus),
                request.Status))
        {
            return AdminAccountStatusUpdateResult.Failure(
                AdminAccountStatusUpdateFailureReason.InvalidInput);
        }

        var target =
            await _dbContext.Users
                .SingleOrDefaultAsync(
                    user => user.Id == targetUserId,
                    cancellationToken);

        if (target is null)
        {
            return AdminAccountStatusUpdateResult.Failure(
                AdminAccountStatusUpdateFailureReason.NotFound);
        }

        var roles =
            await (
                from userRole in
                    _dbContext.UserRoles.AsNoTracking()
                join role in
                    _dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where userRole.UserId == targetUserId
                select role.Name)
            .ToListAsync(cancellationToken);

        if (roles.Count != 1 ||
            string.IsNullOrWhiteSpace(roles[0]))
        {
            return AdminAccountStatusUpdateResult.Failure(
                AdminAccountStatusUpdateFailureReason.ProtectedTarget);
        }

        var targetRole = roles[0]!;

        if (targetUserId == administratorUserId ||
            string.Equals(
                targetRole,
                RoleNames.Administrator,
                StringComparison.Ordinal) ||
            (
                !string.Equals(
                    targetRole,
                    RoleNames.JobSeeker,
                    StringComparison.Ordinal) &&
                !string.Equals(
                    targetRole,
                    RoleNames.Employer,
                    StringComparison.Ordinal)
            ))
        {
            return AdminAccountStatusUpdateResult.Failure(
                AdminAccountStatusUpdateFailureReason.ProtectedTarget);
        }

        // Canonical no-op behavior:
        // same state succeeds without a database write and
        // without incrementing TokenVersion.
        if (target.AccountStatus == request.Status)
        {
            return AdminAccountStatusUpdateResult.Success(
                ToDto(
                    target,
                    targetRole));
        }

        _dbContext.Entry(target)
            .Property(user => user.RowVersion)
            .OriginalValue =
                request.RowVersion;

        target.AccountStatus =
            request.Status;

        target.TokenVersion =
            checked(target.TokenVersion + 1);

        target.UpdatedAtUtc =
            _clock.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AdminAccountStatusUpdateResult.Failure(
                AdminAccountStatusUpdateFailureReason
                    .ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return AdminAccountStatusUpdateResult.Failure(
                AdminAccountStatusUpdateFailureReason
                    .PersistenceFailed);
        }

        return AdminAccountStatusUpdateResult.Success(
            ToDto(
                target,
                targetRole));
    }

    private static AdminUserListItemDto ToDto(
        ApplicationUser user,
        string role)
    {
        return new AdminUserListItemDto(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            role,
            user.AccountStatus,
            user.CreatedAtUtc,
            Convert.ToBase64String(
                user.RowVersion));
    }
}