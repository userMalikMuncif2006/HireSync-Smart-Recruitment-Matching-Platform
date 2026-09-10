using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Application.Services;
using HireSync.Domain.Enums;

namespace HireSync.Application.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_returns_token_for_active_user()
    {
        var userId = Guid.NewGuid();

        var identityService = new FakeIdentityService(
            new AuthenticatedIdentity(
                userId,
                "user@example.com",
                RoleNames.JobSeeker,
                2,
                AccountStatus.Active));

        var tokenService = new FakeTokenService();

        var service = new AuthService(
            identityService,
            tokenService);

        var result = await service.LoginAsync(
            new LoginRequest(
                "user@example.com",
                "ValidPassword123!"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(userId, result.Response.UserId);
        Assert.Equal(RoleNames.JobSeeker, result.Response.Role);
        Assert.Equal("test-token", result.Response.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_rejects_invalid_credentials()
    {
        var service = new AuthService(
            new FakeIdentityService(null),
            new FakeTokenService());

        var result = await service.LoginAsync(
            new LoginRequest(
                "user@example.com",
                "wrong-password"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            LoginFailureReason.InvalidCredentials,
            result.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_rejects_suspended_user()
    {
        var identityService = new FakeIdentityService(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "user@example.com",
                RoleNames.Employer,
                1,
                AccountStatus.Suspended));

        var service = new AuthService(
            identityService,
            new FakeTokenService());

        var result = await service.LoginAsync(
            new LoginRequest(
                "user@example.com",
                "ValidPassword123!"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            LoginFailureReason.Suspended,
            result.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_rejects_unactivated_administrator()
    {
        var identityService = new FakeIdentityService(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false));

        var service = new AuthService(
            identityService,
            new FakeTokenService());

        var result = await service.LoginAsync(
            new LoginRequest(
                "admin@example.com",
                "ValidPassword123!"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            LoginFailureReason.AdministratorActivationRequired,
            result.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_allows_activated_administrator()
    {
        var identityService = new FakeIdentityService(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                true));

        var service = new AuthService(
            identityService,
            new FakeTokenService());

        var result = await service.LoginAsync(
            new LoginRequest(
                "admin@example.com",
                "ValidPassword123!"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(
            RoleNames.Administrator,
            result.Response.Role);
    }

    [Fact]
    public async Task LoginAsync_does_not_apply_admin_activation_gate_to_employer()
    {
        var identityService = new FakeIdentityService(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "employer@example.com",
                RoleNames.Employer,
                1,
                AccountStatus.Active,
                false));

        var service = new AuthService(
            identityService,
            new FakeTokenService());

        var result = await service.LoginAsync(
            new LoginRequest(
                "employer@example.com",
                "ValidPassword123!"));

        Assert.True(result.Succeeded);
    }
    private sealed class FakeIdentityService(
        AuthenticatedIdentity? identity)
        : IIdentityService
    {
        public Task<AuthenticatedIdentity?> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(identity);
        }

        public Task<IdentityUserCreationResult> CreateUserAsync(
            string email,
            string password,
            string displayName,
            string role,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                IdentityUserCreationResult.Failure(
                    IdentityUserCreationFailure.ValidationFailed));
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessTokenResult CreateAccessToken(
            Guid userId,
            string email,
            string role,
            int tokenVersion)
        {
            return new AccessTokenResult(
                "test-token",
                new DateTime(
                    2026, 9, 8, 12, 30, 0,
                    DateTimeKind.Utc));
        }
    }
}
