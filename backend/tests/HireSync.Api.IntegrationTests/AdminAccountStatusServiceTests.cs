using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Admin;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Api.IntegrationTests;

public sealed class AdminAccountStatusServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_suspends_job_seeker_and_increments_token_version()
    {
        await using var dbContext =
            CreateDbContext();

        var roleId = Guid.NewGuid();

        dbContext.Roles.Add(
            CreateRole(
                roleId,
                RoleNames.JobSeeker));

        var user =
            CreateUser(
                "Seeker User",
                "seeker@example.com",
                AccountStatus.Active,
                tokenVersion: 3,
                rowVersion:
                    new byte[] { 1, 2, 3 });

        dbContext.Users.Add(user);

        dbContext.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = roleId
            });

        await dbContext.SaveChangesAsync();

        var now =
            Utc(
                2026,
                9,
                13,
                7,
                0);

        var service =
            new AdminAccountStatusService(
                dbContext,
                new FakeClock(now));

        var result =
            await service.UpdateStatusAsync(
                Guid.NewGuid(),
                user.Id,
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Suspended,
                    new byte[] { 1, 2, 3 }));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);

        Assert.Equal(
            AccountStatus.Suspended,
            user.AccountStatus);

        Assert.Equal(
            4,
            user.TokenVersion);

        Assert.Equal(
            now,
            user.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateStatusAsync_reactivates_employer_and_increments_token_version()
    {
        await using var dbContext =
            CreateDbContext();

        var roleId = Guid.NewGuid();

        dbContext.Roles.Add(
            CreateRole(
                roleId,
                RoleNames.Employer));

        var user =
            CreateUser(
                "Employer User",
                "employer@example.com",
                AccountStatus.Suspended,
                tokenVersion: 8,
                rowVersion:
                    new byte[] { 4, 5, 6 });

        dbContext.Users.Add(user);

        dbContext.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = roleId
            });

        await dbContext.SaveChangesAsync();

        var service =
            new AdminAccountStatusService(
                dbContext,
                new FakeClock(
                    Utc(
                        2026,
                        9,
                        13,
                        7,
                        30)));

        var result =
            await service.UpdateStatusAsync(
                Guid.NewGuid(),
                user.Id,
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Active,
                    new byte[] { 4, 5, 6 }));

        Assert.True(result.Succeeded);

        Assert.Equal(
            AccountStatus.Active,
            user.AccountStatus);

        Assert.Equal(
            9,
            user.TokenVersion);
    }

    [Fact]
    public async Task UpdateStatusAsync_same_state_is_no_op()
    {
        await using var dbContext =
            CreateDbContext();

        var roleId = Guid.NewGuid();

        dbContext.Roles.Add(
            CreateRole(
                roleId,
                RoleNames.JobSeeker));

        var user =
            CreateUser(
                "Same State",
                "same@example.com",
                AccountStatus.Active,
                tokenVersion: 5,
                rowVersion:
                    new byte[] { 1 });

        dbContext.Users.Add(user);

        dbContext.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = roleId
            });

        await dbContext.SaveChangesAsync();

        var originalUpdatedAt =
            user.UpdatedAtUtc;

        var service =
            new AdminAccountStatusService(
                dbContext,
                new FakeClock(
                    Utc(
                        2026,
                        9,
                        13,
                        8,
                        0)));

        var result =
            await service.UpdateStatusAsync(
                Guid.NewGuid(),
                user.Id,
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Active,
                    new byte[] { 9, 9, 9 }));

        Assert.True(result.Succeeded);

        Assert.Equal(
            5,
            user.TokenVersion);

        Assert.Equal(
            originalUpdatedAt,
            user.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateStatusAsync_rejects_current_admin_target()
    {
        await using var dbContext =
            CreateDbContext();

        var roleId = Guid.NewGuid();

        dbContext.Roles.Add(
            CreateRole(
                roleId,
                RoleNames.Administrator));

        var administrator =
            CreateUser(
                "Current Administrator",
                "admin@example.com",
                AccountStatus.Active,
                tokenVersion: 1,
                rowVersion:
                    new byte[] { 1 });

        dbContext.Users.Add(
            administrator);

        dbContext.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId = administrator.Id,
                RoleId = roleId
            });

        await dbContext.SaveChangesAsync();

        var service =
            new AdminAccountStatusService(
                dbContext,
                new FakeClock(
                    Utc(
                        2026,
                        9,
                        13,
                        8,
                        30)));

        var result =
            await service.UpdateStatusAsync(
                administrator.Id,
                administrator.Id,
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Suspended,
                    new byte[] { 1 }));

        Assert.False(result.Succeeded);

        Assert.Equal(
            AdminAccountStatusUpdateFailureReason
                .ProtectedTarget,
            result.FailureReason);

        Assert.Equal(
            AccountStatus.Active,
            administrator.AccountStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_rejects_another_administrator()
    {
        await using var dbContext =
            CreateDbContext();

        var roleId = Guid.NewGuid();

        dbContext.Roles.Add(
            CreateRole(
                roleId,
                RoleNames.Administrator));

        var administrator =
            CreateUser(
                "Protected Administrator",
                "protected@example.com",
                AccountStatus.Active,
                tokenVersion: 1,
                rowVersion:
                    new byte[] { 2 });

        dbContext.Users.Add(
            administrator);

        dbContext.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId = administrator.Id,
                RoleId = roleId
            });

        await dbContext.SaveChangesAsync();

        var service =
            new AdminAccountStatusService(
                dbContext,
                new FakeClock(
                    Utc(
                        2026,
                        9,
                        13,
                        9,
                        0)));

        var result =
            await service.UpdateStatusAsync(
                Guid.NewGuid(),
                administrator.Id,
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Suspended,
                    new byte[] { 2 }));

        Assert.False(result.Succeeded);

        Assert.Equal(
            AdminAccountStatusUpdateFailureReason
                .ProtectedTarget,
            result.FailureReason);
    }

    [Fact]
    public async Task UpdateStatusAsync_returns_not_found_for_missing_user()
    {
        await using var dbContext =
            CreateDbContext();

        var service =
            new AdminAccountStatusService(
                dbContext,
                new FakeClock(
                    Utc(
                        2026,
                        9,
                        13,
                        9,
                        30)));

        var result =
            await service.UpdateStatusAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Suspended,
                    new byte[] { 1 }));

        Assert.False(result.Succeeded);

        Assert.Equal(
            AdminAccountStatusUpdateFailureReason
                .NotFound,
            result.FailureReason);
    }

    [Fact]
    public async Task UpdateStatusAsync_returns_concurrency_conflict_for_stale_row_version()
    {
        await using var dbContext =
            CreateDbContext();

        var roleId = Guid.NewGuid();

        dbContext.Roles.Add(
            CreateRole(
                roleId,
                RoleNames.JobSeeker));

        var user =
            CreateUser(
                "Concurrency User",
                "concurrency@example.com",
                AccountStatus.Active,
                tokenVersion: 2,
                rowVersion:
                    new byte[] { 7, 7, 7 });

        dbContext.Users.Add(user);

        dbContext.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = roleId
            });

        await dbContext.SaveChangesAsync();

        var service =
            new AdminAccountStatusService(
                dbContext,
                new FakeClock(
                    Utc(
                        2026,
                        9,
                        13,
                        10,
                        0)));

        var result =
            await service.UpdateStatusAsync(
                Guid.NewGuid(),
                user.Id,
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Suspended,
                    new byte[] { 8, 8, 8 }));

        Assert.False(result.Succeeded);

        Assert.Equal(
            AdminAccountStatusUpdateFailureReason
                .ConcurrencyConflict,
            result.FailureReason);
    }

    private static HireSyncDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<HireSyncDbContext>()
                .UseInMemoryDatabase(
                    $"admin-account-status-{Guid.NewGuid()}")
                .Options;

        return new HireSyncDbContext(
            options);
    }

    private static IdentityRole<Guid> CreateRole(
        Guid id,
        string role)
    {
        return new IdentityRole<Guid>
        {
            Id = id,
            Name = role,
            NormalizedName =
                role.ToUpperInvariant()
        };
    }

    private static ApplicationUser CreateUser(
        string displayName,
        string email,
        AccountStatus status,
        int tokenVersion,
        byte[] rowVersion)
    {
        var createdAt =
            Utc(
                2026,
                9,
                13,
                6,
                0);

        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName =
                email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail =
                email.ToUpperInvariant(),
            DisplayName = displayName,
            AccountStatus = status,
            TokenVersion = tokenVersion,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt,
            RowVersion = rowVersion
        };
    }

    private static DateTime Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        return new DateTime(
            year,
            month,
            day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(
            DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}