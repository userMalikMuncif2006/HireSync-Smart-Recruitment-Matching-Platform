using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Application.Services;
using HireSync.Domain.Enums;

namespace HireSync.Application.Tests;

public sealed class JobSeekerEmailVerificationAuthTests
{
    [Fact]
    public async Task LoginAsync_rejects_unverified_job_seeker()
    {
        var service = new AuthService(
            new FakeIdentityService(
                new AuthenticatedIdentity(
                    Guid.NewGuid(),
                    "seeker@example.com",
                    RoleNames.JobSeeker,
                    1,
                    AccountStatus.Active,
                    false)),
            new FakeTokenService());

        var result = await service.LoginAsync(
            new LoginRequest(
                "seeker@example.com",
                "ValidPassword123!"));

        Assert.False(result.Succeeded);

        Assert.Equal(
            LoginFailureReason.JobSeekerEmailVerificationRequired,
            result.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_allows_verified_job_seeker()
    {
        var service = new AuthService(
            new FakeIdentityService(
                new AuthenticatedIdentity(
                    Guid.NewGuid(),
                    "seeker@example.com",
                    RoleNames.JobSeeker,
                    1,
                    AccountStatus.Active,
                    true)),
            new FakeTokenService());

        var result = await service.LoginAsync(
            new LoginRequest(
                "seeker@example.com",
                "ValidPassword123!"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);

        Assert.Equal(
            RoleNames.JobSeeker,
            result.Response.Role);
    }

    private sealed class FakeIdentityService(
        AuthenticatedIdentity identity)
        : IIdentityService
    {
        public Task<AuthenticatedIdentity?> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthenticatedIdentity?>(
                identity);
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
                "jobseeker-test-token",
                new DateTime(
                    2026,
                    9,
                    14,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc));
        }
    }
}