using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Security;
using HireSync.Application.Services;
using HireSync.Domain.Enums;

namespace HireSync.Application.Tests;

public class AdministratorActivationServiceTests
{
    [Fact]
    public async Task RequestOtpAsync_uses_administrator_first_activation_purpose()
    {
        var otp = new FakeEmailOtpService();

        var service = CreateService(
            AdministratorIdentity(emailConfirmed: false),
            otp);

        var result = await service.RequestOtpAsync(
            new AdministratorActivationRequest(
                "admin@example.com",
                "ValidPassword123!"));

        Assert.True(result.Succeeded);
        Assert.Equal(
            EmailOtpPurpose.AdministratorFirstActivation,
            otp.LastRequestPurpose);
        Assert.Equal(
            "admin@example.com",
            otp.LastRequestEmail);
    }

    [Fact]
    public async Task RequestOtpAsync_rejects_invalid_credentials()
    {
        var service = CreateService(
            identity: null,
            new FakeEmailOtpService());

        var result = await service.RequestOtpAsync(
            new AdministratorActivationRequest(
                "admin@example.com",
                "wrong"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AdministratorActivationRequestFailureReason.InvalidCredentials,
            result.FailureReason);
    }

    [Fact]
    public async Task RequestOtpAsync_rejects_non_administrator()
    {
        var identity = new AuthenticatedIdentity(
            Guid.NewGuid(),
            "user@example.com",
            RoleNames.JobSeeker,
            1,
            AccountStatus.Active,
            false);

        var service = CreateService(
            identity,
            new FakeEmailOtpService());

        var result = await service.RequestOtpAsync(
            new AdministratorActivationRequest(
                "user@example.com",
                "ValidPassword123!"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AdministratorActivationRequestFailureReason.InvalidCredentials,
            result.FailureReason);
    }

    [Fact]
    public async Task RequestOtpAsync_rejects_already_activated_administrator()
    {
        var service = CreateService(
            AdministratorIdentity(emailConfirmed: true),
            new FakeEmailOtpService());

        var result = await service.RequestOtpAsync(
            new AdministratorActivationRequest(
                "admin@example.com",
                "ValidPassword123!"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AdministratorActivationRequestFailureReason.AlreadyActivated,
            result.FailureReason);
    }

    [Fact]
    public async Task RequestOtpAsync_maps_cooldown()
    {
        var otp = new FakeEmailOtpService
        {
            RequestResult = OtpRequestResult.Cooldown(45)
        };

        var service = CreateService(
            AdministratorIdentity(emailConfirmed: false),
            otp);

        var result = await service.RequestOtpAsync(
            new AdministratorActivationRequest(
                "admin@example.com",
                "ValidPassword123!"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AdministratorActivationRequestFailureReason.CooldownActive,
            result.FailureReason);
        Assert.Equal(45, result.RetryAfterSeconds);
    }

    [Fact]
    public async Task VerifyOtpAsync_activates_administrator_for_valid_code()
    {
        var completer = new FakeActivationCompleter();

        var service = CreateService(
            AdministratorIdentity(emailConfirmed: false),
            new FakeEmailOtpService(),
            completer);

        var result = await service.VerifyOtpAsync(
            new AdministratorActivationVerifyRequest(
                "admin@example.com",
                "ValidPassword123!",
                "123456"));

        Assert.True(result.Succeeded);
        Assert.Equal(1, completer.CallCount);
        Assert.Equal(
            EmailOtpPurpose.AdministratorFirstActivation,
            completer.OtpService.LastVerificationPurpose);
    }

    [Fact]
    public async Task VerifyOtpAsync_does_not_activate_for_invalid_code()
    {
        var otp = new FakeEmailOtpService
        {
            VerificationResult =
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.InvalidCode)
        };

        var completer = new FakeActivationCompleter();

        var service = CreateService(
            AdministratorIdentity(emailConfirmed: false),
            otp,
            completer);

        var result = await service.VerifyOtpAsync(
            new AdministratorActivationVerifyRequest(
                "admin@example.com",
                "ValidPassword123!",
                "000000"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AdministratorActivationVerificationFailureReason.InvalidCode,
            result.FailureReason);
        Assert.Equal(0, completer.CallCount);
    }

    [Fact]
    public async Task VerifyOtpAsync_rejects_suspended_administrator()
    {
        var identity = new AuthenticatedIdentity(
            Guid.NewGuid(),
            "admin@example.com",
            RoleNames.Administrator,
            1,
            AccountStatus.Suspended,
            false);

        var service = CreateService(
            identity,
            new FakeEmailOtpService());

        var result = await service.VerifyOtpAsync(
            new AdministratorActivationVerifyRequest(
                "admin@example.com",
                "ValidPassword123!",
                "123456"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AdministratorActivationVerificationFailureReason.Suspended,
            result.FailureReason);
    }

    [Fact]
    public async Task VerifyOtpAsync_reports_persistence_failure()
    {
        var completer = new FakeActivationCompleter
        {
            Result = false
        };

        var service = CreateService(
            AdministratorIdentity(emailConfirmed: false),
            new FakeEmailOtpService(),
            completer);

        var result = await service.VerifyOtpAsync(
            new AdministratorActivationVerifyRequest(
                "admin@example.com",
                "ValidPassword123!",
                "123456"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AdministratorActivationVerificationFailureReason.PersistenceFailed,
            result.FailureReason);
    }

    private static AuthenticatedIdentity AdministratorIdentity(
        bool emailConfirmed)
    {
        return new AuthenticatedIdentity(
            Guid.NewGuid(),
            "admin@example.com",
            RoleNames.Administrator,
            1,
            AccountStatus.Active,
            emailConfirmed);
    }

    private static AdministratorActivationService CreateService(
        AuthenticatedIdentity? identity,
        FakeEmailOtpService otp,
        FakeActivationCompleter? completer = null)
    {
        completer ??= new FakeActivationCompleter();
        completer.OtpService = otp;

        return new AdministratorActivationService(
            new FakeIdentityService(identity),
            otp,
            completer);
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

    private sealed class FakeEmailOtpService : IEmailOtpService
    {
        public OtpRequestResult RequestResult { get; set; } =
            OtpRequestResult.Success(
                new DateTime(
                    2026, 9, 9, 12, 0, 0,
                    DateTimeKind.Utc));

        public OtpVerificationResult VerificationResult { get; set; } =
            OtpVerificationResult.Success();

        public string? LastRequestEmail { get; private set; }

        public EmailOtpPurpose? LastRequestPurpose { get; private set; }

        public EmailOtpPurpose? LastVerificationPurpose { get; private set; }

        public Task<OtpRequestResult> RequestAsync(
            string email,
            EmailOtpPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            LastRequestEmail = email;
            LastRequestPurpose = purpose;
            return Task.FromResult(RequestResult);
        }

        public Task<OtpVerificationResult> VerifyAsync(
            string email,
            EmailOtpPurpose purpose,
            string code,
            CancellationToken cancellationToken = default)
        {
            LastVerificationPurpose = purpose;
            return Task.FromResult(VerificationResult);
        }
    }

    private sealed class FakeActivationCompleter
        : IAdministratorActivationCompleter
    {
        public bool Result { get; set; } = true;

        public int CallCount { get; private set; }

        public FakeEmailOtpService OtpService { get; set; } =
            new();

        public Task<bool> MarkActivatedAsync(
            Guid administratorUserId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Result);
        }
    }
}
