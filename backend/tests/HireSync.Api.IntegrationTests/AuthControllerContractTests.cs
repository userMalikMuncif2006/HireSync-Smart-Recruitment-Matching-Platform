using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Application.Services;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Http;
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

    [Fact]
    public async Task RequestEmployerOtp_returns_200_and_uses_employer_registration_purpose()
    {
        var otpService = new FakeEmailOtpService();

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.RequestEmployerOtp(
            new EmployerOtpRequest("employer@example.com"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<OtpRequestResult>(ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.True(response.Succeeded);
        Assert.Equal(
            EmailOtpPurpose.EmployerRegistration,
            otpService.LastRequestPurpose);
        Assert.Equal(
            "employer@example.com",
            otpService.LastRequestEmail);
    }

    [Fact]
    public async Task RequestEmployerOtp_returns_429_during_cooldown()
    {
        var otpService = new FakeEmailOtpService
        {
            RequestResult = OtpRequestResult.Cooldown(30)
        };

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.RequestEmployerOtp(
            new EmployerOtpRequest("employer@example.com"),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(429, problem.StatusCode);
        Assert.Equal(
            "30",
            controller.Response.Headers["Retry-After"].ToString());

        Assert.Equal(
            EmailOtpPurpose.EmployerRegistration,
            otpService.LastRequestPurpose);
    }

    [Fact]
    public async Task RequestEmployerOtp_returns_400_when_email_is_missing()
    {
        var otpService = new FakeEmailOtpService();

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.RequestEmployerOtp(
            new EmployerOtpRequest(""),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(0, otpService.RequestCallCount);
    }
    [Fact]
    public async Task VerifyEmployerOtp_returns_200_for_valid_code_and_uses_employer_registration_purpose()
    {
        var otpService = new FakeEmailOtpService();

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.VerifyEmployerOtp(
            new EmployerOtpVerifyRequest(
                "employer@example.com",
                "123456"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response =
            Assert.IsType<OtpVerificationResult>(ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.True(response.Succeeded);

        Assert.Equal(
            EmailOtpPurpose.EmployerRegistration,
            otpService.LastVerificationPurpose);

        Assert.Equal(
            "123456",
            otpService.LastVerificationCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_400_for_invalid_code()
    {
        var otpService = new FakeEmailOtpService
        {
            VerificationResult =
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.InvalidCode)
        };

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.VerifyEmployerOtp(
            new EmployerOtpVerifyRequest(
                "employer@example.com",
                "000000"),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_400_for_expired_code()
    {
        var otpService = new FakeEmailOtpService
        {
            VerificationResult =
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.Expired)
        };

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.VerifyEmployerOtp(
            new EmployerOtpVerifyRequest(
                "employer@example.com",
                "123456"),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_409_for_consumed_code()
    {
        var otpService = new FakeEmailOtpService
        {
            VerificationResult =
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.AlreadyUsed)
        };

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.VerifyEmployerOtp(
            new EmployerOtpVerifyRequest(
                "employer@example.com",
                "123456"),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_429_when_attempt_limit_is_reached()
    {
        var otpService = new FakeEmailOtpService
        {
            VerificationResult =
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.AttemptsExceeded)
        };

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.VerifyEmployerOtp(
            new EmployerOtpVerifyRequest(
                "employer@example.com",
                "123456"),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(429, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_400_when_request_is_incomplete()
    {
        var otpService = new FakeEmailOtpService();

        var controller = CreateController(
            identity: null,
            otpService: otpService);

        var result = await controller.VerifyEmployerOtp(
            new EmployerOtpVerifyRequest(
                "",
                ""),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(0, otpService.VerificationCallCount);
    }
    [Fact]
    public async Task Login_returns_403_for_unactivated_administrator()
    {
        var controller = CreateController(
            identity: new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false));

        var result = await controller.Login(
            new LoginRequest(
                "admin@example.com",
                "ValidPassword123!"),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(403, problem.StatusCode);
    }
    private static AuthController CreateController(
        AuthenticatedIdentity? identity,
        IdentityUserCreationResult? creationResult = null,
        FakeEmailOtpService? otpService = null)
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

        var controller = new AuthController(
            authService,
            registrationService,
            otpService ?? new FakeEmailOtpService());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
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
