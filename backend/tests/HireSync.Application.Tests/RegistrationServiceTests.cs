using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Security;
using HireSync.Application.Services;

namespace HireSync.Application.Tests;

public class RegistrationServiceTests
{
    [Fact]
    public async Task RegisterJobSeekerAsync_creates_JobSeeker_account()
    {
        var userId = Guid.NewGuid();

        var identityService = new FakeIdentityService(
            IdentityUserCreationResult.Success(
                userId,
                "seeker@example.com",
                "Test Seeker"));

        var service = new RegistrationService(identityService);

        var result = await service.RegisterJobSeekerAsync(
            new RegisterJobSeekerRequest(
                " seeker@example.com ",
                "ValidPassword123!",
                " Test Seeker "));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal(userId, result.Response.UserId);
        Assert.Equal("seeker@example.com", result.Response.Email);
        Assert.Equal("Test Seeker", result.Response.DisplayName);
        Assert.Equal(RoleNames.JobSeeker, result.Response.Role);

        Assert.Equal(RoleNames.JobSeeker, identityService.LastRole);
        Assert.Equal("seeker@example.com", identityService.LastEmail);
        Assert.Equal("Test Seeker", identityService.LastDisplayName);
    }

    [Fact]
    public async Task RegisterJobSeekerAsync_rejects_duplicate_email()
    {
        var service = new RegistrationService(
            new FakeIdentityService(
                IdentityUserCreationResult.Failure(
                    IdentityUserCreationFailure.EmailAlreadyExists)));

        var result = await service.RegisterJobSeekerAsync(
            new RegisterJobSeekerRequest(
                "existing@example.com",
                "ValidPassword123!",
                "Existing User"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            RegistrationFailureReason.EmailAlreadyExists,
            result.FailureReason);
    }

    [Fact]
    public async Task RegisterJobSeekerAsync_rejects_blank_input()
    {
        var service = new RegistrationService(
            new FakeIdentityService(
                IdentityUserCreationResult.Failure(
                    IdentityUserCreationFailure.ValidationFailed)));

        var result = await service.RegisterJobSeekerAsync(
            new RegisterJobSeekerRequest(
                "",
                "ValidPassword123!",
                "Test User"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            RegistrationFailureReason.InvalidInput,
            result.FailureReason);
    }

    private sealed class FakeIdentityService(
        IdentityUserCreationResult creationResult)
        : IIdentityService
    {
        public string? LastEmail { get; private set; }

        public string? LastDisplayName { get; private set; }

        public string? LastRole { get; private set; }

        public Task<AuthenticatedIdentity?> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthenticatedIdentity?>(null);
        }

        public Task<IdentityUserCreationResult> CreateUserAsync(
            string email,
            string password,
            string displayName,
            string role,
            CancellationToken cancellationToken = default)
        {
            LastEmail = email;
            LastDisplayName = displayName;
            LastRole = role;

            return Task.FromResult(creationResult);
        }
    }
}
