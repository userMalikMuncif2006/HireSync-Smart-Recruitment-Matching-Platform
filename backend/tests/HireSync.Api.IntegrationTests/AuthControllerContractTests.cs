using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Application.Services;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public class AuthControllerContractTests
{
    [Fact]
    public async Task Login_returns_200_for_active_valid_user()
    {
        var userId = Guid.NewGuid();

        var controller = CreateController(
            identity: new AuthenticatedIdentity(
                userId,
                "user@example.com",
                RoleNames.JobSeeker,
                1,
                AccountStatus.Active));

        var result = await controller.Login(
            new LoginRequest(
                "user@example.com",
                "ValidPassword123!"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(RoleNames.JobSeeker, response.Role);
    }

    [Fact]
    public async Task Login_returns_401_for_invalid_credentials()
    {
        var controller = CreateController(identity: null);

        var result = await controller.Login(
            new LoginRequest(
                "user@example.com",
                "wrong-password"),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(401, problem.StatusCode);
    }

    [Fact]
    public async Task Login_returns_403_for_suspended_user()
    {
        var controller = CreateController(
            identity: new AuthenticatedIdentity(
                Guid.NewGuid(),
                "employer@example.com",
                RoleNames.Employer,
                1,
                AccountStatus.Suspended));

        var result = await controller.Login(
            new LoginRequest(
                "employer@example.com",
                "ValidPassword123!"),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(403, problem.StatusCode);
    }

    [Fact]
    public async Task RegisterJobSeeker_returns_201_for_success()
    {
        var userId = Guid.NewGuid();

        var controller = CreateController(
            identity: null,
            creationResult: IdentityUserCreationResult.Success(
                userId,
                "seeker@example.com",
                "Test Seeker"));

        var result = await controller.RegisterJobSeeker(
            new RegisterJobSeekerRequest(
                "seeker@example.com",
                "ValidPassword123!",
                "Test Seeker"),
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        var response =
            Assert.IsType<RegisterJobSeekerResponse>(created.Value);

        Assert.Equal(201, created.StatusCode);
        Assert.Equal(userId, response.UserId);
        Assert.Equal("seeker@example.com", response.Email);
        Assert.Equal(RoleNames.JobSeeker, response.Role);
    }

    [Fact]
    public async Task RegisterJobSeeker_returns_409_for_duplicate_email()
    {
        var controller = CreateController(
            identity: null,
            creationResult: IdentityUserCreationResult.Failure(
                IdentityUserCreationFailure.EmailAlreadyExists));

        var result = await controller.RegisterJobSeeker(
            new RegisterJobSeekerRequest(
                "existing@example.com",
                "ValidPassword123!",
                "Existing User"),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task RegisterJobSeeker_returns_400_for_invalid_input()
    {
        var controller = CreateController(identity: null);

        var result = await controller.RegisterJobSeeker(
            new RegisterJobSeekerRequest(
                "",
                "ValidPassword123!",
                "Test User"),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    private static AuthController CreateController(
        AuthenticatedIdentity? identity,
        IdentityUserCreationResult? creationResult = null)
    {
        var identityService = new FakeIdentityService(
            identity,
            creationResult ??
            IdentityUserCreationResult.Failure(
                IdentityUserCreationFailure.ValidationFailed));

        var authService = new AuthService(
            identityService,
            new FakeTokenService());

        var registrationService =
            new RegistrationService(identityService);

        return new AuthController(
            authService,
            registrationService);
    }

    private sealed class FakeIdentityService(
        AuthenticatedIdentity? identity,
        IdentityUserCreationResult creationResult)
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
            return Task.FromResult(creationResult);
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
