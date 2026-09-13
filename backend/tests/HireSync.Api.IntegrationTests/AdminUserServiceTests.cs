using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Admin;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Api.IntegrationTests;

public sealed class AdminUserServiceTests
{
    [Fact]
    public async Task GetUsersAsync_returns_safe_paged_identity_projection()
    {
        await using var dbContext =
            CreateDbContext();

        var jobSeekerRoleId = Guid.NewGuid();
        var employerRoleId = Guid.NewGuid();

        dbContext.Roles.AddRange(
            CreateRole(
                jobSeekerRoleId,
                RoleNames.JobSeeker),
            CreateRole(
                employerRoleId,
                RoleNames.Employer));

        var firstUser =
            CreateUser(
                "Alice Seeker",
                "alice@example.com",
                AccountStatus.Active,
                new byte[] { 1, 2, 3 });

        var secondUser =
            CreateUser(
                "Bob Employer",
                "bob@example.com",
                AccountStatus.Suspended,
                new byte[] { 4, 5, 6 });

        dbContext.Users.AddRange(
            firstUser,
            secondUser);

        dbContext.UserRoles.AddRange(
            new IdentityUserRole<Guid>
            {
                UserId = firstUser.Id,
                RoleId = jobSeekerRoleId
            },
            new IdentityUserRole<Guid>
            {
                UserId = secondUser.Id,
                RoleId = employerRoleId
            });

        await dbContext.SaveChangesAsync();

        var service =
            new AdminUserService(dbContext);

        var result =
            await service.GetUsersAsync(
                search: null,
                status: null,
                page: 1,
                pageSize: 20);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.Items.Count);

        var alice =
            Assert.Single(
                result.Items,
                item =>
                    item.Email ==
                    "alice@example.com");

        Assert.Equal(
            RoleNames.JobSeeker,
            alice.Role);

        Assert.Equal(
            AccountStatus.Active,
            alice.AccountStatus);

        Assert.Equal(
            "AQID",
            alice.RowVersion);
    }

    [Fact]
    public async Task GetUsersAsync_applies_search_and_status_filter()
    {
        await using var dbContext =
            CreateDbContext();

        var jobSeekerRoleId = Guid.NewGuid();
        var employerRoleId = Guid.NewGuid();

        dbContext.Roles.AddRange(
            CreateRole(
                jobSeekerRoleId,
                RoleNames.JobSeeker),
            CreateRole(
                employerRoleId,
                RoleNames.Employer));

        var activeUser =
            CreateUser(
                "Active Seeker",
                "active@example.com",
                AccountStatus.Active,
                new byte[] { 1 });

        var suspendedUser =
            CreateUser(
                "Suspended Employer",
                "target@example.com",
                AccountStatus.Suspended,
                new byte[] { 2 });

        dbContext.Users.AddRange(
            activeUser,
            suspendedUser);

        dbContext.UserRoles.AddRange(
            new IdentityUserRole<Guid>
            {
                UserId = activeUser.Id,
                RoleId = jobSeekerRoleId
            },
            new IdentityUserRole<Guid>
            {
                UserId = suspendedUser.Id,
                RoleId = employerRoleId
            });

        await dbContext.SaveChangesAsync();

        var service =
            new AdminUserService(dbContext);

        var result =
            await service.GetUsersAsync(
                search: "target@example.com",
                status: AccountStatus.Suspended,
                page: 1,
                pageSize: 20);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            suspendedUser.Id,
            item.Id);

        Assert.Equal(
            AccountStatus.Suspended,
            item.AccountStatus);

        Assert.Equal(
            RoleNames.Employer,
            item.Role);
    }

    [Fact]
    public async Task GetUsersAsync_applies_paging_after_filters()
    {
        await using var dbContext =
            CreateDbContext();

        var roleId = Guid.NewGuid();

        dbContext.Roles.Add(
            CreateRole(
                roleId,
                RoleNames.JobSeeker));

        var users =
            new[]
            {
                CreateUser(
                    "Alpha User",
                    "alpha@example.com",
                    AccountStatus.Active,
                    new byte[] { 1 }),
                CreateUser(
                    "Beta User",
                    "beta@example.com",
                    AccountStatus.Active,
                    new byte[] { 2 }),
                CreateUser(
                    "Gamma User",
                    "gamma@example.com",
                    AccountStatus.Active,
                    new byte[] { 3 })
            };

        dbContext.Users.AddRange(users);

        dbContext.UserRoles.AddRange(
            users.Select(
                user =>
                    new IdentityUserRole<Guid>
                    {
                        UserId = user.Id,
                        RoleId = roleId
                    }));

        await dbContext.SaveChangesAsync();

        var service =
            new AdminUserService(dbContext);

        var result =
            await service.GetUsersAsync(
                search: null,
                status: AccountStatus.Active,
                page: 2,
                pageSize: 2);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            "Gamma User",
            item.DisplayName);
    }

    private static HireSyncDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<HireSyncDbContext>()
                .UseInMemoryDatabase(
                    $"admin-users-{Guid.NewGuid()}")
                .Options;

        return new HireSyncDbContext(options);
    }

    private static IdentityRole<Guid> CreateRole(
        Guid id,
        string roleName)
    {
        return new IdentityRole<Guid>
        {
            Id = id,
            Name = roleName,
            NormalizedName =
                roleName.ToUpperInvariant()
        };
    }

    private static ApplicationUser CreateUser(
        string displayName,
        string email,
        AccountStatus accountStatus,
        byte[] rowVersion)
    {
        var now =
            new DateTime(
                2026,
                9,
                13,
                6,
                0,
                0,
                DateTimeKind.Utc);

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
            AccountStatus = accountStatus,
            TokenVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RowVersion = rowVersion
        };
    }
}