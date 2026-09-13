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
    public async Task RequestEmployerOtp_returns_200_for_registered_unverified_employer()
    {
        var verification =
            new FakeEmployerEmailVerificationService
            {
                RequestResult =
                    EmployerEmailVerificationRequestResult.Success(
                        new DateTime(
                            2026,
                            9,
                            12,
                            3,
                            10,
                            0,
                            DateTimeKind.Utc))
            };

        var controller = CreateController(
            identity: null,
            employerEmailVerificationService: verification);

        var result =
            await controller.RequestEmployerOtp(
                new EmployerOtpRequest(
                    "employer@example.com"),
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<OtpRequestResult>(
                ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.True(response.Succeeded);
        Assert.Equal(1, verification.RequestCallCount);

        Assert.Equal(
            "employer@example.com",
            verification.LastRequestEmail);
    }

    [Fact]
    public async Task RequestEmployerOtp_returns_429_during_cooldown()
    {
        var verification =
            new FakeEmployerEmailVerificationService
            {
                RequestResult =
                    EmployerEmailVerificationRequestResult.Cooldown(
                        45)
            };

        var controller = CreateController(
            identity: null,
            employerEmailVerificationService: verification);

        var result =
            await controller.RequestEmployerOtp(
                new EmployerOtpRequest(
                    "employer@example.com"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(429, problem.StatusCode);

        Assert.Equal(
            "45",
            controller.Response.Headers["Retry-After"].ToString());
    }

    [Fact]
    public async Task RequestEmployerOtp_returns_400_when_email_is_missing()
    {
        var verification =
            new FakeEmployerEmailVerificationService
            {
                RequestResult =
                    EmployerEmailVerificationRequestResult.Failure(
                        EmployerEmailVerificationRequestFailureReason.InvalidRequest)
            };

        var controller = CreateController(
            identity: null,
            employerEmailVerificationService: verification);

        var result =
            await controller.RequestEmployerOtp(
                new EmployerOtpRequest(""),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(1, verification.RequestCallCount);
    }

    [Fact]
    public async Task RequestEmployerOtp_returns_400_for_unknown_employer()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    RequestResult =
                        EmployerEmailVerificationRequestResult.Failure(
                            EmployerEmailVerificationRequestFailureReason.InvalidAccount)
                });

        var result =
            await controller.RequestEmployerOtp(
                new EmployerOtpRequest(
                    "unknown@example.com"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task RequestEmployerOtp_returns_403_for_suspended_employer()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    RequestResult =
                        EmployerEmailVerificationRequestResult.Failure(
                            EmployerEmailVerificationRequestFailureReason.Suspended)
                });

        var result =
            await controller.RequestEmployerOtp(
                new EmployerOtpRequest(
                    "employer@example.com"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(403, problem.StatusCode);
    }

    [Fact]
    public async Task RequestEmployerOtp_returns_409_for_already_verified_employer()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    RequestResult =
                        EmployerEmailVerificationRequestResult.Failure(
                            EmployerEmailVerificationRequestFailureReason.AlreadyVerified)
                });

        var result =
            await controller.RequestEmployerOtp(
                new EmployerOtpRequest(
                    "employer@example.com"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task RequestEmployerOtp_returns_400_when_otp_request_fails()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    RequestResult =
                        EmployerEmailVerificationRequestResult.Failure(
                            EmployerEmailVerificationRequestFailureReason.RequestFailed)
                });

        var result =
            await controller.RequestEmployerOtp(
                new EmployerOtpRequest(
                    "employer@example.com"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_200_for_valid_code()
    {
        var verification =
            new FakeEmployerEmailVerificationService
            {
                Result =
                    EmployerEmailVerificationResult.Success()
            };

        var controller = CreateController(
            identity: null,
            employerEmailVerificationService: verification);

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "employer@example.com",
                    "123456"),
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<EmployerEmailVerificationResult>(
                ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.True(response.Succeeded);
        Assert.Equal(1, verification.CallCount);
        Assert.Equal(
            "employer@example.com",
            verification.LastEmail);
        Assert.Equal(
            "123456",
            verification.LastCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_400_for_invalid_code()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    Result =
                        EmployerEmailVerificationResult.Failure(
                            EmployerEmailVerificationFailureReason.InvalidCode)
                });

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "employer@example.com",
                    "000000"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_400_for_expired_code()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    Result =
                        EmployerEmailVerificationResult.Failure(
                            EmployerEmailVerificationFailureReason.Expired)
                });

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "employer@example.com",
                    "123456"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_409_for_consumed_code()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    Result =
                        EmployerEmailVerificationResult.Failure(
                            EmployerEmailVerificationFailureReason.AlreadyUsed)
                });

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "employer@example.com",
                    "123456"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_429_when_attempt_limit_is_reached()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    Result =
                        EmployerEmailVerificationResult.Failure(
                            EmployerEmailVerificationFailureReason.AttemptsExceeded)
                });

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "employer@example.com",
                    "123456"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(429, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_400_for_invalid_request()
    {
        var verification =
            new FakeEmployerEmailVerificationService
            {
                Result =
                    EmployerEmailVerificationResult.Failure(
                        EmployerEmailVerificationFailureReason.InvalidRequest)
            };

        var controller = CreateController(
            identity: null,
            employerEmailVerificationService: verification);

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "",
                    ""),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(1, verification.CallCount);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_403_for_suspended_employer()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    Result =
                        EmployerEmailVerificationResult.Failure(
                            EmployerEmailVerificationFailureReason.Suspended)
                });

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "employer@example.com",
                    "123456"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(403, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_409_when_email_is_already_verified()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    Result =
                        EmployerEmailVerificationResult.Failure(
                            EmployerEmailVerificationFailureReason.AlreadyVerified)
                });

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "employer@example.com",
                    "123456"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_400_for_invalid_employer_account()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    Result =
                        EmployerEmailVerificationResult.Failure(
                            EmployerEmailVerificationFailureReason.InvalidAccount)
                });

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "unknown@example.com",
                    "123456"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task VerifyEmployerOtp_returns_500_for_persistence_failure()
    {
        var controller = CreateController(
            identity: null,
            employerEmailVerificationService:
                new FakeEmployerEmailVerificationService
                {
                    Result =
                        EmployerEmailVerificationResult.Failure(
                            EmployerEmailVerificationFailureReason.PersistenceFailed)
                });

        var result =
            await controller.VerifyEmployerOtp(
                new EmployerOtpVerifyRequest(
                    "employer@example.com",
                    "123456"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(500, problem.StatusCode);
    }

    [Fact]
    public async Task Login_returns_403_for_unverified_employer()
    {
        var controller = CreateController(
            identity: new AuthenticatedIdentity(
                Guid.NewGuid(),
                "employer@example.com",
                RoleNames.Employer,
                1,
                AccountStatus.Active,
                false));

        var result =
            await controller.Login(
                new LoginRequest(
                    "employer@example.com",
                    "ValidPassword123!"),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(403, problem.StatusCode);
    }

    [Fact]
    public async Task Login_returns_200_for_verified_employer()
    {
        var controller = CreateController(
            identity: new AuthenticatedIdentity(
                Guid.NewGuid(),
                "employer@example.com",
                RoleNames.Employer,
                1,
                AccountStatus.Active,
                true));

        var result =
            await controller.Login(
                new LoginRequest(
                    "employer@example.com",
                    "ValidPassword123!"),
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<LoginResponse>(
                ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(
            RoleNames.Employer,
            response.Role);
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
    [Fact]
    public async Task RequestJobSeekerOtp_returns_200_for_unverified_job_seeker()
    {
        var verification =
            new FakeJobSeekerEmailVerificationService
            {
                RequestResult =
                    JobSeekerEmailVerificationRequestResult.Success(
                        new DateTime(
                            2026,
                            9,
                            14,
                            1,
                            0,
                            0,
                            DateTimeKind.Utc))
            };

        var controller = CreateController(
            identity: null,
            jobSeekerEmailVerificationService: verification);

        var result = await controller.RequestJobSeekerOtp(
            new JobSeekerOtpRequest(
                "seeker@example.com"),
            CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(1, verification.RequestCallCount);
        Assert.Equal(
            "seeker@example.com",
            verification.LastRequestEmail);
    }

    [Fact]
    public async Task VerifyJobSeekerOtp_returns_200_for_valid_code()
    {
        var verification =
            new FakeJobSeekerEmailVerificationService
            {
                Result =
                    JobSeekerEmailVerificationResult.Success()
            };

        var controller = CreateController(
            identity: null,
            jobSeekerEmailVerificationService: verification);

        var result = await controller.VerifyJobSeekerOtp(
            new JobSeekerOtpVerifyRequest(
                "seeker@example.com",
                "123456"),
            CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(1, verification.VerifyCallCount);
        Assert.Equal(
            "seeker@example.com",
            verification.LastVerifyEmail);
        Assert.Equal(
            "123456",
            verification.LastCode);
    }

    [Fact]
    public async Task Login_returns_403_for_unverified_job_seeker()
    {
        var controller = CreateController(
            identity: new AuthenticatedIdentity(
                Guid.NewGuid(),
                "seeker@example.com",
                RoleNames.JobSeeker,
                1,
                AccountStatus.Active,
                false));

        var result = await controller.Login(
            new LoginRequest(
                "seeker@example.com",
                "ValidPassword123!"),
            CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(403, problem.StatusCode);
    }

    [Fact]
    public async Task Login_returns_200_for_verified_job_seeker()
    {
        var controller = CreateController(
            identity: new AuthenticatedIdentity(
                Guid.NewGuid(),
                "seeker@example.com",
                RoleNames.JobSeeker,
                1,
                AccountStatus.Active,
                true));

        var result = await controller.Login(
            new LoginRequest(
                "seeker@example.com",
                "ValidPassword123!"),
            CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<LoginResponse>(
                ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(
            RoleNames.JobSeeker,
            response.Role);
    }
    private static AuthController CreateController(
        AuthenticatedIdentity? identity,
        IdentityUserCreationResult? creationResult = null,
        FakeEmailOtpService? otpService = null,
        FakeAdministratorActivationCompleter? activationCompleter = null,
        EmployerRegistrationResult? employerRegistrationResult = null,
        FakeEmployerEmailVerificationService? employerEmailVerificationService = null,
        FakeJobSeekerEmailVerificationService? jobSeekerEmailVerificationService = null)
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

        var resolvedEmployerEmailVerificationService =
            employerEmailVerificationService ??
            new FakeEmployerEmailVerificationService();

        var resolvedJobSeekerEmailVerificationService =
            jobSeekerEmailVerificationService ??
            new FakeJobSeekerEmailVerificationService();

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
            resolvedEmployerEmailVerificationService,
            resolvedJobSeekerEmailVerificationService,
            administratorActivationService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private sealed class FakeEmployerEmailVerificationService
        : IEmployerEmailVerificationService
    {
        public EmployerEmailVerificationResult Result { get; set; } =
            EmployerEmailVerificationResult.Failure(
                EmployerEmailVerificationFailureReason.InvalidRequest);

        public int CallCount { get; private set; }

        public string? LastEmail { get; private set; }

        public string? LastCode { get; private set; }

        public EmployerEmailVerificationRequestResult RequestResult { get; set; } =
            EmployerEmailVerificationRequestResult.Failure(
                EmployerEmailVerificationRequestFailureReason.InvalidRequest);

        public int RequestCallCount { get; private set; }

        public string? LastRequestEmail { get; private set; }

        public Task<EmployerEmailVerificationRequestResult> RequestAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            RequestCallCount++;
            LastRequestEmail = email;

            return Task.FromResult(
                RequestResult);
        }

        public Task<EmployerEmailVerificationResult> VerifyAsync(
            string email,
            string code,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastEmail = email;
            LastCode = code;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class FakeJobSeekerEmailVerificationService
        : IJobSeekerEmailVerificationService
    {
        public JobSeekerEmailVerificationRequestResult RequestResult { get; set; } =
            JobSeekerEmailVerificationRequestResult.Failure(
                JobSeekerEmailVerificationRequestFailureReason.InvalidRequest);

        public JobSeekerEmailVerificationResult Result { get; set; } =
            JobSeekerEmailVerificationResult.Failure(
                JobSeekerEmailVerificationFailureReason.InvalidRequest);

        public int RequestCallCount { get; private set; }

        public string? LastRequestEmail { get; private set; }

        public int VerifyCallCount { get; private set; }

        public string? LastVerifyEmail { get; private set; }

        public string? LastCode { get; private set; }

        public Task<JobSeekerEmailVerificationRequestResult> RequestAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            RequestCallCount++;
            LastRequestEmail = email;

            return Task.FromResult(
                RequestResult);
        }

        public Task<JobSeekerEmailVerificationResult> VerifyAsync(
            string email,
            string code,
            CancellationToken cancellationToken = default)
        {
            VerifyCallCount++;
            LastVerifyEmail = email;
            LastCode = code;

            return Task.FromResult(
                Result);
        }
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
