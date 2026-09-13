using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class AccessTokenStateValidatorTests
{
    [Fact]
    public async Task Old_token_stays_invalid_after_suspend_and_reactivate()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<HireSyncDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"access-token-state-{Guid.NewGuid()}"));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<HireSyncDbContext>();

        await using var provider =
            services.BuildServiceProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole<Guid>>>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var roleResult =
            await roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = RoleNames.JobSeeker
                });

        Assert.True(roleResult.Succeeded);

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "token-test@example.com",
                Email = "token-test@example.com",
                EmailConfirmed = true,
                DisplayName = "Token Test User",
                AccountStatus = AccountStatus.Active,
                TokenVersion = 1,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

        var createResult =
            await userManager.CreateAsync(user);

        Assert.True(createResult.Succeeded);

        var addRoleResult =
            await userManager.AddToRoleAsync(
                user,
                RoleNames.JobSeeker);

        Assert.True(addRoleResult.Succeeded);

        var validator =
            new AccessTokenStateValidator(
                userManager);

        Assert.True(
            await validator.IsValidAsync(
                user.Id,
                RoleNames.JobSeeker,
                tokenVersion: 1));

        user.AccountStatus =
            AccountStatus.Suspended;

        user.TokenVersion = 2;

        var suspendResult =
            await userManager.UpdateAsync(user);

        Assert.True(suspendResult.Succeeded);

        Assert.False(
            await validator.IsValidAsync(
                user.Id,
                RoleNames.JobSeeker,
                tokenVersion: 1));

        user.AccountStatus =
            AccountStatus.Active;

        user.TokenVersion = 3;

        var reactivateResult =
            await userManager.UpdateAsync(user);

        Assert.True(reactivateResult.Succeeded);

        Assert.False(
            await validator.IsValidAsync(
                user.Id,
                RoleNames.JobSeeker,
                tokenVersion: 1));

        Assert.True(
            await validator.IsValidAsync(
                user.Id,
                RoleNames.JobSeeker,
                tokenVersion: 3));
    }
}