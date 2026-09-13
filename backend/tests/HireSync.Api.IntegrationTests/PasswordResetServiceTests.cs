using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HireSync.Api.IntegrationTests;

public sealed class PasswordResetServiceTests
{
    [Fact]
    public async Task RequestAsync_accepts_known_and_unknown_email_without_enumeration()
    {
        using var environment =
            await CreateEnvironmentAsync();

        await CreateUserAsync(
            environment,
            "known@example.com");

        var otp =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock);

        var service =
            CreateService(
                environment,
                otp);

        var knownResult =
            await service.RequestAsync(
                " known@example.com ");

        var unknownResult =
            await service.RequestAsync(
                "unknown@example.com");

        Assert.True(knownResult);
        Assert.True(unknownResult);

        Assert.Equal(
            1,
            otp.RequestCallCount);

        Assert.Equal(
            "known@example.com",
            otp.LastRequestEmail);

        Assert.Equal(
            EmailOtpPurpose.PasswordReset,
            otp.LastRequestPurpose);
    }

    [Fact]
    public async Task RequestAsync_hides_otp_cooldown_from_caller()
    {
        using var environment =
            await CreateEnvironmentAsync();

        await CreateUserAsync(
            environment,
            "cooldown@example.com");

        var otp =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock,
                requestResult:
                    OtpRequestResult.Cooldown(
                        45));

        var service =
            CreateService(
                environment,
                otp);

        var result =
            await service.RequestAsync(
                "cooldown@example.com");

        Assert.True(result);

        Assert.Equal(
            EmailOtpPurpose.PasswordReset,
            otp.LastRequestPurpose);
    }

    [Fact]
    public async Task CompleteAsync_success_resets_password_consumes_otp_and_increments_token_version()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var user =
            await CreateUserAsync(
                environment,
                "reset@example.com",
                tokenVersion: 7);

        await CreateChallengeAsync(
            environment,
            user.Email!);

        var otp =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock);

        var service =
            CreateService(
                environment,
                otp);

        var result =
            await service.CompleteAsync(
                new PasswordResetCompleteRequest(
                    "reset@example.com",
                    "123456",
                    "NewValidPassword456!"));

        Assert.True(result.Succeeded);

        environment.Context.ChangeTracker.Clear();

        var persistedUser =
            await environment.Context.Users
                .SingleAsync(candidate =>
                    candidate.Id == user.Id);

        Assert.Equal(
            8,
            persistedUser.TokenVersion);

        Assert.Equal(
            environment.Clock.UtcNow,
            persistedUser.UpdatedAtUtc);

        Assert.False(
            await environment.UserManager
                .CheckPasswordAsync(
                    persistedUser,
                    "ValidPassword123!"));

        Assert.True(
            await environment.UserManager
                .CheckPasswordAsync(
                    persistedUser,
                    "NewValidPassword456!"));

        var challenge =
            await environment.Context
                .EmailOtpChallenges
                .SingleAsync(candidate =>
                    candidate.Email ==
                        "reset@example.com" &&
                    candidate.Purpose ==
                        EmailOtpPurpose.PasswordReset);

        Assert.True(
            challenge.IsConsumed);

        Assert.Equal(
            EmailOtpPurpose.PasswordReset,
            otp.LastVerificationPurpose);
    }

    [Fact]
    public async Task CompleteAsync_weak_password_rolls_back_otp_consumption_and_password_change()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var user =
            await CreateUserAsync(
                environment,
                "weak@example.com",
                tokenVersion: 3);

        await CreateChallengeAsync(
            environment,
            user.Email!);

        var otp =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock);

        var service =
            CreateService(
                environment,
                otp);

        var result =
            await service.CompleteAsync(
                new PasswordResetCompleteRequest(
                    "weak@example.com",
                    "123456",
                    "weak"));

        Assert.False(result.Succeeded);

        Assert.Equal(
            PasswordResetCompleteFailureReason
                .InvalidPassword,
            result.FailureReason);

        Assert.NotEmpty(
            result.PasswordErrors);

        environment.Context.ChangeTracker.Clear();

        var persistedUser =
            await environment.Context.Users
                .SingleAsync(candidate =>
                    candidate.Id == user.Id);

        Assert.Equal(
            3,
            persistedUser.TokenVersion);

        Assert.True(
            await environment.UserManager
                .CheckPasswordAsync(
                    persistedUser,
                    "ValidPassword123!"));

        var challenge =
            await environment.Context
                .EmailOtpChallenges
                .SingleAsync(candidate =>
                    candidate.Email ==
                        "weak@example.com" &&
                    candidate.Purpose ==
                        EmailOtpPurpose.PasswordReset);

        Assert.False(
            challenge.IsConsumed);
    }

    [Fact]
    public async Task CompleteAsync_invalid_code_persists_failed_attempt_without_changing_password()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var user =
            await CreateUserAsync(
                environment,
                "invalid@example.com",
                tokenVersion: 5);

        await CreateChallengeAsync(
            environment,
            user.Email!);

        var otp =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock,
                verificationResult:
                    OtpVerificationResult.Failure(
                        OtpVerificationFailureReason
                            .InvalidCode),
                registerFailedAttempt: true);

        var service =
            CreateService(
                environment,
                otp);

        var result =
            await service.CompleteAsync(
                new PasswordResetCompleteRequest(
                    "invalid@example.com",
                    "000000",
                    "NewValidPassword456!"));

        Assert.False(result.Succeeded);

        Assert.Equal(
            PasswordResetCompleteFailureReason
                .InvalidOrExpiredCode,
            result.FailureReason);

        environment.Context.ChangeTracker.Clear();

        var challenge =
            await environment.Context
                .EmailOtpChallenges
                .SingleAsync(candidate =>
                    candidate.Email ==
                        "invalid@example.com" &&
                    candidate.Purpose ==
                        EmailOtpPurpose.PasswordReset);

        Assert.False(
            challenge.IsConsumed);

        Assert.Equal(
            1,
            challenge.FailedAttempts);

        var persistedUser =
            await environment.Context.Users
                .SingleAsync(candidate =>
                    candidate.Id == user.Id);

        Assert.Equal(
            5,
            persistedUser.TokenVersion);

        Assert.True(
            await environment.UserManager
                .CheckPasswordAsync(
                    persistedUser,
                    "ValidPassword123!"));
    }

    [Fact]
    public async Task CompleteAsync_unknown_email_returns_generic_code_failure()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var otp =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock,
                verificationResult:
                    OtpVerificationResult.Failure(
                        OtpVerificationFailureReason
                            .InvalidCode));

        var service =
            CreateService(
                environment,
                otp);

        var result =
            await service.CompleteAsync(
                new PasswordResetCompleteRequest(
                    "unknown@example.com",
                    "123456",
                    "NewValidPassword456!"));

        Assert.False(result.Succeeded);

        Assert.Equal(
            PasswordResetCompleteFailureReason
                .InvalidOrExpiredCode,
            result.FailureReason);
    }

    [Fact]
    public async Task CompleteAsync_preserves_account_status_and_email_confirmation()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var user =
            await CreateUserAsync(
                environment,
                "suspended@example.com",
                accountStatus:
                    AccountStatus.Suspended,
                emailConfirmed: false);

        await CreateChallengeAsync(
            environment,
            user.Email!);

        var otp =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock);

        var service =
            CreateService(
                environment,
                otp);

        var result =
            await service.CompleteAsync(
                new PasswordResetCompleteRequest(
                    "suspended@example.com",
                    "123456",
                    "NewValidPassword456!"));

        Assert.True(result.Succeeded);

        environment.Context.ChangeTracker.Clear();

        var persistedUser =
            await environment.Context.Users
                .SingleAsync(candidate =>
                    candidate.Id == user.Id);

        Assert.Equal(
            AccountStatus.Suspended,
            persistedUser.AccountStatus);

        Assert.False(
            persistedUser.EmailConfirmed);
    }

    private static PasswordResetService CreateService(
        TestEnvironment environment,
        IEmailOtpService otpService)
    {
        return new PasswordResetService(
            environment.Context,
            environment.UserManager,
            otpService,
            environment.Clock,
            NullLogger<PasswordResetService>
                .Instance);
    }

    private static async Task<ApplicationUser>
        CreateUserAsync(
            TestEnvironment environment,
            string email,
            int tokenVersion = 1,
            AccountStatus accountStatus =
                AccountStatus.Active,
            bool emailConfirmed = true)
    {
        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed =
                    emailConfirmed,
                DisplayName =
                    "Password Reset Test User",
                AccountStatus =
                    accountStatus,
                TokenVersion =
                    tokenVersion,
                CreatedAtUtc =
                    environment.Clock.UtcNow
                        .AddDays(-1),
                UpdatedAtUtc =
                    environment.Clock.UtcNow
                        .AddDays(-1)
            };

        var creation =
            await environment.UserManager
                .CreateAsync(
                    user,
                    "ValidPassword123!");

        Assert.True(
            creation.Succeeded,
            string.Join(
                "; ",
                creation.Errors.Select(
                    error =>
                        error.Description)));

        return user;
    }

    private static async Task<EmailOtpChallenge>
        CreateChallengeAsync(
            TestEnvironment environment,
            string email)
    {
        var challenge =
            new EmailOtpChallenge(
                Guid.NewGuid(),
                email,
                EmailOtpPurpose.PasswordReset,
                "password-reset-test-hash",
                environment.Clock.UtcNow,
                environment.Clock.UtcNow
                    .AddMinutes(5));

        await environment.Context
            .EmailOtpChallenges
            .AddAsync(
                challenge);

        await environment.Context
            .SaveChangesAsync();

        return challenge;
    }

    private static async Task<TestEnvironment>
        CreateEnvironmentAsync()
    {
        var server =
            Environment.GetEnvironmentVariable(
                "HIRESYNC_TEST_SQLSERVER");

        if (string.IsNullOrWhiteSpace(
            server))
        {
            server = @".\SQLEXPRESS";
        }

        var databaseName =
            $"HireSyncPasswordResetTests_{Guid.NewGuid():N}";

        var connectionString =
            $"Server={server};" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;" +
            "MultipleActiveResultSets=True";

        var services =
            new ServiceCollection();

        services.AddLogging();
        services.AddDataProtection();

        services.AddDbContext<HireSyncDbContext>(
            options =>
                options.UseSqlServer(
                    connectionString));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<HireSyncDbContext>()
            .AddDefaultTokenProviders();

        var provider =
            services.BuildServiceProvider();

        var scope =
            provider.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<
                    HireSyncDbContext>();

        await context.Database
            .EnsureCreatedAsync();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        return new TestEnvironment(
            provider,
            scope,
            context,
            userManager,
            new FakeClock());
    }

    private sealed class TransactionalFakeOtpService
        : IEmailOtpService
    {
        private readonly HireSyncDbContext
            _context;

        private readonly IClock _clock;

        private readonly OtpRequestResult
            _requestResult;

        private readonly OtpVerificationResult
            _verificationResult;

        private readonly bool
            _registerFailedAttempt;

        public TransactionalFakeOtpService(
            HireSyncDbContext context,
            IClock clock,
            OtpRequestResult? requestResult = null,
            OtpVerificationResult? verificationResult = null,
            bool registerFailedAttempt = false)
        {
            _context = context;
            _clock = clock;

            _requestResult =
                requestResult ??
                OtpRequestResult.Success(
                    clock.UtcNow
                        .AddMinutes(5));

            _verificationResult =
                verificationResult ??
                OtpVerificationResult.Success();

            _registerFailedAttempt =
                registerFailedAttempt;
        }

        public int RequestCallCount
        {
            get;
            private set;
        }

        public string? LastRequestEmail
        {
            get;
            private set;
        }

        public EmailOtpPurpose?
            LastRequestPurpose
        {
            get;
            private set;
        }

        public EmailOtpPurpose?
            LastVerificationPurpose
        {
            get;
            private set;
        }

        public Task<OtpRequestResult>
            RequestAsync(
                string email,
                EmailOtpPurpose purpose,
                CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            RequestCallCount++;

            LastRequestEmail =
                email;

            LastRequestPurpose =
                purpose;

            return Task.FromResult(
                _requestResult);
        }

        public async Task<OtpVerificationResult>
            VerifyAsync(
                string email,
                EmailOtpPurpose purpose,
                string code,
                CancellationToken cancellationToken = default)
        {
            LastVerificationPurpose =
                purpose;

            var challenge =
                await _context
                    .EmailOtpChallenges
                    .Where(candidate =>
                        candidate.Email ==
                            email.Trim() &&
                        candidate.Purpose ==
                            purpose)
                    .OrderByDescending(candidate =>
                        candidate.CreatedAtUtc)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (challenge is null)
            {
                return OtpVerificationResult
                    .Failure(
                        OtpVerificationFailureReason
                            .InvalidCode);
            }

            if (!_verificationResult.Succeeded)
            {
                if (_registerFailedAttempt)
                {
                    challenge.RegisterFailedAttempt(
                        EmailOtpPolicy
                            .MaxFailedAttempts);

                    await _context
                        .SaveChangesAsync(
                            cancellationToken);
                }

                return _verificationResult;
            }

            challenge.MarkConsumed(
                _clock.UtcNow);

            await _context
                .SaveChangesAsync(
                    cancellationToken);

            return _verificationResult;
        }
    }

    private sealed class FakeClock
        : IClock
    {
        public DateTime UtcNow { get; } =
            new(
                2026,
                9,
                14,
                3,
                0,
                0,
                DateTimeKind.Utc);
    }

    private sealed class TestEnvironment
        : IDisposable
    {
        private readonly ServiceProvider
            _provider;

        private readonly IServiceScope
            _scope;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context,
            UserManager<ApplicationUser>
                userManager,
            FakeClock clock)
        {
            _provider = provider;
            _scope = scope;

            Context = context;
            UserManager = userManager;
            Clock = clock;
        }

        public HireSyncDbContext Context
        {
            get;
        }

        public UserManager<ApplicationUser>
            UserManager
        {
            get;
        }

        public FakeClock Clock
        {
            get;
        }

        public void Dispose()
        {
            try
            {
                Context.Database
                    .EnsureDeleted();
            }
            finally
            {
                _scope.Dispose();
                _provider.Dispose();
            }
        }
    }
}
