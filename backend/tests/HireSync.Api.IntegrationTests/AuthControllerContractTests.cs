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
    public async Task RegisterEmployer_returns_201_for_success()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var controller = CreateController(
            identity: null,
            employerRegistrationResult:
                EmployerRegistrationResult.Success(
                    new RegisterEmployerResponse(
                        userId,
                        profileId,
                        "employer@example.com",
                        RoleNames.Employer,
                        EmployerVerificationStatus.Pending)));

        var result =
            await controller.RegisterEmployer(
                CreateValidEmployerRegistrationRequest(),
                CancellationToken.None);

        var created =
            Assert.IsType<ObjectResult>(
                result.Result);

        var response =
            Assert.IsType<RegisterEmployerResponse>(
                created.Value);

        Assert.Equal(201, created.StatusCode);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(profileId, response.EmployerProfileId);
        Assert.Equal(RoleNames.Employer, response.Role);

        Assert.Equal(
            EmployerVerificationStatus.Pending,
            response.EmployerVerificationStatus);
    }

    [Fact]
    public async Task RegisterEmployer_returns_400_for_invalid_input()
    {
        var controller =
            CreateController(identity: null);

        var request =
            CreateValidEmployerRegistrationRequest() with
            {
                CompanyName = "A"
            };

        var result =
            await controller.RegisterEmployer(
                request,
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task RegisterEmployer_returns_409_for_duplicate_email()
    {
        var controller = CreateController(
            identity: null,
            employerRegistrationResult:
                EmployerRegistrationResult.Failure(
                    EmployerRegistrationFailureReason
                        .EmailAlreadyExists));

        var result =
            await controller.RegisterEmployer(
                CreateValidEmployerRegistrationRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task RegisterEmployer_returns_409_for_duplicate_brn()
    {
        var controller = CreateController(
            identity: null,
            employerRegistrationResult:
                EmployerRegistrationResult.Failure(
                    EmployerRegistrationFailureReason
                        .DuplicateBusinessRegistrationNumber));

        var result =
            await controller.RegisterEmployer(
                CreateValidEmployerRegistrationRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task RegisterEmployer_returns_400_for_identity_validation_failure()
    {
        var controller = CreateController(
            identity: null,
            employerRegistrationResult:
                EmployerRegistrationResult.Failure(
                    EmployerRegistrationFailureReason
                        .IdentityValidationFailed));

        var result =
            await controller.RegisterEmployer(
                CreateValidEmployerRegistrationRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task RegisterEmployer_returns_500_for_persistence_failure()
    {
        var controller = CreateController(
            identity: null,
            employerRegistrationResult:
                EmployerRegistrationResult.Failure(
                    EmployerRegistrationFailureReason
                        .PersistenceFailed));

        var result =
            await controller.RegisterEmployer(
                CreateValidEmployerRegistrationRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(500, problem.StatusCode);
    }

    private static RegisterEmployerRequest
        CreateValidEmployerRegistrationRequest()
    {
        return new RegisterEmployerRequest(
            "employer@example.com",
            "ValidPassword123!",
            "Example Holdings",
            "A complete Employer organisation profile description.",
            "Colombo",
            "Test Contact",
            "HR Manager",
            "PV 12345",
            "0771234567",
            "https://example.com");
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
    [Fact]
    public async Task RequestAdministratorActivationOtp_returns_200_and_uses_admin_activation_purpose()
    {
        var otp = new FakeEmailOtpService();

        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false),
            otpService: otp);

        var result =
            await controller.RequestAdministratorActivationOtp(
                new AdministratorActivationRequest(
                    "admin@example.com",
                    "ValidPassword123!"),
                CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(
            EmailOtpPurpose.AdministratorFirstActivation,
            otp.LastRequestPurpose);
    }

    [Fact]
    public async Task RequestAdministratorActivationOtp_returns_401_for_invalid_credentials()
    {
        var controller = CreateController(identity: null);

        var result =
            await controller.RequestAdministratorActivationOtp(
                new AdministratorActivationRequest(
                    "admin@example.com",
                    "wrong"),
                CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(401, problem.StatusCode);
    }

    [Fact]
    public async Task RequestAdministratorActivationOtp_returns_403_for_suspended_admin()
    {
        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Suspended,
                false));

        var result =
            await controller.RequestAdministratorActivationOtp(
                new AdministratorActivationRequest(
                    "admin@example.com",
                    "ValidPassword123!"),
                CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(403, problem.StatusCode);
    }

    [Fact]
    public async Task RequestAdministratorActivationOtp_returns_409_when_already_activated()
    {
        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                true));

        var result =
            await controller.RequestAdministratorActivationOtp(
                new AdministratorActivationRequest(
                    "admin@example.com",
                    "ValidPassword123!"),
                CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task RequestAdministratorActivationOtp_returns_429_during_cooldown()
    {
        var otp = new FakeEmailOtpService
        {
            RequestResult = OtpRequestResult.Cooldown(30)
        };

        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false),
            otpService: otp);

        var result =
            await controller.RequestAdministratorActivationOtp(
                new AdministratorActivationRequest(
                    "admin@example.com",
                    "ValidPassword123!"),
                CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(429, problem.StatusCode);
        Assert.Equal(
            "30",
            controller.Response.Headers["Retry-After"].ToString());
    }

    [Fact]
    public async Task VerifyAdministratorActivationOtp_returns_200_for_valid_code()
    {
        var otp = new FakeEmailOtpService();
        var completer =
            new FakeAdministratorActivationCompleter();

        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false),
            otpService: otp,
            activationCompleter: completer);

        var result =
            await controller.VerifyAdministratorActivationOtp(
                new AdministratorActivationVerifyRequest(
                    "admin@example.com",
                    "ValidPassword123!",
                    "123456"),
                CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(1, completer.CallCount);
        Assert.Equal(
            EmailOtpPurpose.AdministratorFirstActivation,
            otp.LastVerificationPurpose);
    }

    [Fact]
    public async Task VerifyAdministratorActivationOtp_returns_400_for_invalid_code()
    {
        var otp = new FakeEmailOtpService
        {
            VerificationResult =
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.InvalidCode)
        };

        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false),
            otpService: otp);

        var result =
            await controller.VerifyAdministratorActivationOtp(
                new AdministratorActivationVerifyRequest(
                    "admin@example.com",
                    "ValidPassword123!",
                    "000000"),
                CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyAdministratorActivationOtp_returns_409_for_consumed_code()
    {
        var otp = new FakeEmailOtpService
        {
            VerificationResult =
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.AlreadyUsed)
        };

        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false),
            otpService: otp);

        var result =
            await controller.VerifyAdministratorActivationOtp(
                new AdministratorActivationVerifyRequest(
                    "admin@example.com",
                    "ValidPassword123!",
                    "123456"),
                CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyAdministratorActivationOtp_returns_429_when_attempt_limit_reached()
    {
        var otp = new FakeEmailOtpService
        {
            VerificationResult =
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.AttemptsExceeded)
        };

        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false),
            otpService: otp);

        var result =
            await controller.VerifyAdministratorActivationOtp(
                new AdministratorActivationVerifyRequest(
                    "admin@example.com",
                    "ValidPassword123!",
                    "123456"),
                CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(429, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyAdministratorActivationOtp_returns_500_when_activation_cannot_be_saved()
    {
        var completer =
            new FakeAdministratorActivationCompleter
            {
                Result = false
            };

        var controller = CreateController(
            new AuthenticatedIdentity(
                Guid.NewGuid(),
                "admin@example.com",
                RoleNames.Administrator,
                1,
                AccountStatus.Active,
                false),
            activationCompleter: completer);

        var result =
            await controller.VerifyAdministratorActivationOtp(
                new AdministratorActivationVerifyRequest(
                    "admin@example.com",
                    "ValidPassword123!",
                    "123456"),
                CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(500, problem.StatusCode);
    }
    private static AuthController CreateController(
        AuthenticatedIdentity? identity,
        IdentityUserCreationResult? creationResult = null,
        FakeEmailOtpService? otpService = null,
        FakeAdministratorActivationCompleter? activationCompleter = null,
        EmployerRegistrationResult? employerRegistrationResult = null)
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

        var employerProvisioner =
            new FakeEmployerRegistrationProvisioner(
                employerRegistrationResult ??
                EmployerRegistrationResult.Failure(
                    EmployerRegistrationFailureReason.InvalidInput));

        var employerRegistrationService =
            new EmployerRegistrationService(
                employerProvisioner);

        var resolvedOtpService =
            otpService ?? new FakeEmailOtpService();

        var resolvedActivationCompleter =
            activationCompleter ??
            new FakeAdministratorActivationCompleter();

        var administratorActivationService =
            new AdministratorActivationService(
                identityService,
                resolvedOtpService,
                resolvedActivationCompleter);

        var controller = new AuthController(
            authService,
            registrationService,
            employerRegistrationService,
            resolvedOtpService,
            administratorActivationService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private sealed class FakeEmployerRegistrationProvisioner
        : IEmployerRegistrationProvisioner
    {
        private readonly EmployerRegistrationResult _result;

        public FakeEmployerRegistrationProvisioner(
            EmployerRegistrationResult result)
        {
            _result = result;
        }

        public Task<EmployerRegistrationResult> ProvisionAsync(
            RegisterEmployerRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeAdministratorActivationCompleter
        : IAdministratorActivationCompleter
    {
        public bool Result { get; set; } = true;

        public int CallCount { get; private set; }

        public Task<bool> MarkActivatedAsync(
            Guid administratorUserId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Result);
        }
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
