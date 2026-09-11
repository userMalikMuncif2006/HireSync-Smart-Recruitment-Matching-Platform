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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class EmployerEmailVerificationServiceTests
{
    [Fact]
    public async Task VerifyAsync_confirms_employer_and_consumes_otp_atomically()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var user =
            await CreateEmployerAsync(
                environment,
                "employer@example.com");

        var challenge =
            await CreateChallengeAsync(
                environment,
                "employer@example.com");

        var otpService =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock,
                OtpVerificationResult.Success());

        var service =
            CreateService(
                environment,
                otpService);

        var result =
            await service.VerifyAsync(
                " employer@example.com ",
                "123456");

        Assert.True(result.Succeeded);

        environment.Context.ChangeTracker.Clear();

        var storedUser =
            await environment.Context.Users
                .SingleAsync(
                    candidate =>
                        candidate.Id == user.Id);

        var storedChallenge =
            await environment.Context.EmailOtpChallenges
                .SingleAsync(
                    candidate =>
                        candidate.Id == challenge.Id);

        Assert.True(storedUser.EmailConfirmed);

        Assert.Equal(
            environment.Clock.UtcNow,
            storedUser.UpdatedAtUtc);

        Assert.True(storedChallenge.IsConsumed);

        Assert.Equal(
            environment.Clock.UtcNow,
            storedChallenge.ConsumedAtUtc);

        Assert.Equal(
            EmailOtpPurpose.EmployerRegistration,
            otpService.LastPurpose);
    }

    [Fact]
    public async Task VerifyAsync_rolls_back_consumed_otp_when_identity_update_fails()
    {
        using var environment =
            await CreateEnvironmentAsync(
                new FailEmployerConfirmationSaveInterceptor());

        var user =
            await CreateEmployerAsync(
                environment,
                "rollback@example.com");

        var challenge =
            await CreateChallengeAsync(
                environment,
                "rollback@example.com");

        var otpService =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock,
                OtpVerificationResult.Success());

        var service =
            CreateService(
                environment,
                otpService);

        var result =
            await service.VerifyAsync(
                "rollback@example.com",
                "123456");

        Assert.False(result.Succeeded);

        Assert.Equal(
            EmployerEmailVerificationFailureReason.PersistenceFailed,
            result.FailureReason);

        environment.Context.ChangeTracker.Clear();

        var storedUser =
            await environment.Context.Users
                .SingleAsync(
                    candidate =>
                        candidate.Id == user.Id);

        var storedChallenge =
            await environment.Context.EmailOtpChallenges
                .SingleAsync(
                    candidate =>
                        candidate.Id == challenge.Id);

        Assert.False(storedUser.EmailConfirmed);
        Assert.False(storedChallenge.IsConsumed);
        Assert.Null(storedChallenge.ConsumedAtUtc);
    }

    [Fact]
    public async Task VerifyAsync_preserves_failed_otp_attempt_state()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var user =
            await CreateEmployerAsync(
                environment,
                "invalid-code@example.com");

        var challenge =
            await CreateChallengeAsync(
                environment,
                "invalid-code@example.com");

        var otpService =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock,
                OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.InvalidCode),
                registerFailedAttempt: true);

        var service =
            CreateService(
                environment,
                otpService);

        var result =
            await service.VerifyAsync(
                "invalid-code@example.com",
                "000000");

        Assert.False(result.Succeeded);

        Assert.Equal(
            EmployerEmailVerificationFailureReason.InvalidCode,
            result.FailureReason);

        environment.Context.ChangeTracker.Clear();

        var storedUser =
            await environment.Context.Users
                .SingleAsync(
                    candidate =>
                        candidate.Id == user.Id);

        var storedChallenge =
            await environment.Context.EmailOtpChallenges
                .SingleAsync(
                    candidate =>
                        candidate.Id == challenge.Id);

        Assert.False(storedUser.EmailConfirmed);
        Assert.False(storedChallenge.IsConsumed);
        Assert.Equal(1, storedChallenge.FailedAttempts);
    }

    [Fact]
    public async Task VerifyAsync_rejects_non_employer_account_without_consuming_otp()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var user =
            await CreateUserAsync(
                environment,
                "seeker@example.com",
                RoleNames.JobSeeker);

        var challenge =
            await CreateChallengeAsync(
                environment,
                "seeker@example.com");

        var otpService =
            new TransactionalFakeOtpService(
                environment.Context,
                environment.Clock,
                OtpVerificationResult.Success());

        var service =
            CreateService(
                environment,
                otpService);

        var result =
            await service.VerifyAsync(
                "seeker@example.com",
                "123456");

        Assert.False(result.Succeeded);

        Assert.Equal(
            EmployerEmailVerificationFailureReason.InvalidAccount,
            result.FailureReason);

        Assert.Equal(0, otpService.VerifyCallCount);

        environment.Context.ChangeTracker.Clear();

        var storedUser =
            await environment.Context.Users
                .SingleAsync(
                    candidate =>
                        candidate.Id == user.Id);

        var storedChallenge =
            await environment.Context.EmailOtpChallenges
                .SingleAsync(
                    candidate =>
                        candidate.Id == challenge.Id);

        Assert.False(storedUser.EmailConfirmed);
        Assert.False(storedChallenge.IsConsumed);
    }

    private static EmployerEmailVerificationService CreateService(
        TestEnvironment environment,
        IEmailOtpService otpService)
    {
        return new EmployerEmailVerificationService(
            environment.Context,
            environment.UserManager,
            otpService,
            environment.Clock);
    }

    private static Task<ApplicationUser> CreateEmployerAsync(
        TestEnvironment environment,
        string email)
    {
        return CreateUserAsync(
            environment,
            email,
            RoleNames.Employer);
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        TestEnvironment environment,
        string email,
        string role)
    {
        if (!await environment.RoleManager.RoleExistsAsync(role))
        {
            var roleResult =
                await environment.RoleManager.CreateAsync(
                    new IdentityRole<Guid>(role));

            Assert.True(
                roleResult.Succeeded,
                string.Join(
                    "; ",
                    roleResult.Errors.Select(
                        error => error.Description)));
        }

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                DisplayName = "Test User",
                AccountStatus = AccountStatus.Active,
                EmployerVerificationStatus =
                    role == RoleNames.Employer
                        ? EmployerVerificationStatus.Pending
                        : null,
                TokenVersion = 1,
                CreatedAtUtc = environment.Clock.UtcNow,
                UpdatedAtUtc = environment.Clock.UtcNow
            };

        var creation =
            await environment.UserManager.CreateAsync(
                user,
                "ValidPassword123!");

        Assert.True(
            creation.Succeeded,
            string.Join(
                "; ",
                creation.Errors.Select(
                    error => error.Description)));

        var roleAssignment =
            await environment.UserManager.AddToRoleAsync(
                user,
                role);

        Assert.True(
            roleAssignment.Succeeded,
            string.Join(
                "; ",
                roleAssignment.Errors.Select(
                    error => error.Description)));

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
                EmailOtpPurpose.EmployerRegistration,
                "test-hash",
                environment.Clock.UtcNow,
                environment.Clock.UtcNow.AddMinutes(10));

        await environment.Context.EmailOtpChallenges.AddAsync(
            challenge);

        await environment.Context.SaveChangesAsync();

        return challenge;
    }

    private static async Task<TestEnvironment>
        CreateEnvironmentAsync(
            IInterceptor? interceptor = null)
    {
        var server =
            Environment.GetEnvironmentVariable(
                "HIRESYNC_TEST_SQLSERVER");

        if (string.IsNullOrWhiteSpace(server))
        {
            server = @".\SQLEXPRESS";
        }

        var databaseName =
            $"HireSyncEmployerEmailVerificationTests_{Guid.NewGuid():N}";

        var connectionString =
            $"Server={server};" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;" +
            "MultipleActiveResultSets=True";

        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<HireSyncDbContext>(
            options =>
            {
                options.UseSqlServer(
                    connectionString);

                if (interceptor is not null)
                {
                    options.AddInterceptors(
                        interceptor);
                }
            });

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<HireSyncDbContext>();

        var provider =
            services.BuildServiceProvider();

        var scope =
            provider.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<HireSyncDbContext>();

        await context.Database.EnsureCreatedAsync();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        return new TestEnvironment(
            provider,
            scope,
            context,
            userManager,
            roleManager,
            new FakeClock());
    }

    private sealed class TransactionalFakeOtpService
        : IEmailOtpService
    {
        private readonly HireSyncDbContext _context;
        private readonly IClock _clock;
        private readonly OtpVerificationResult _verificationResult;
        private readonly bool _registerFailedAttempt;

        public TransactionalFakeOtpService(
            HireSyncDbContext context,
            IClock clock,
            OtpVerificationResult verificationResult,
            bool registerFailedAttempt = false)
        {
            _context = context;
            _clock = clock;
            _verificationResult = verificationResult;
            _registerFailedAttempt = registerFailedAttempt;
        }

        public int VerifyCallCount { get; private set; }

        public EmailOtpPurpose? LastPurpose { get; private set; }

        public Task<OtpRequestResult> RequestAsync(
            string email,
            EmailOtpPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public async Task<OtpVerificationResult> VerifyAsync(
            string email,
            EmailOtpPurpose purpose,
            string code,
            CancellationToken cancellationToken = default)
        {
            VerifyCallCount++;
            LastPurpose = purpose;

            var challenge =
                await _context.EmailOtpChallenges
                    .Where(candidate =>
                        candidate.Email == email &&
                        candidate.Purpose == purpose)
                    .OrderByDescending(candidate =>
                        candidate.CreatedAtUtc)
                    .FirstAsync(cancellationToken);

            if (_verificationResult.Succeeded)
            {
                challenge.MarkConsumed(
                    _clock.UtcNow);

                await _context.SaveChangesAsync(
                    cancellationToken);

                return _verificationResult;
            }

            if (_registerFailedAttempt)
            {
                challenge.RegisterFailedAttempt(
                    EmailOtpPolicy.MaxFailedAttempts);

                await _context.SaveChangesAsync(
                    cancellationToken);
            }

            return _verificationResult;
        }
    }

    private sealed class FailEmployerConfirmationSaveInterceptor
        : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>>
            SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
        {
            var confirmationUpdate =
                eventData.Context?
                    .ChangeTracker
                    .Entries<ApplicationUser>()
                    .Any(entry =>
                        entry.State == EntityState.Modified &&
                        entry.Entity.EmailConfirmed) == true;

            if (confirmationUpdate)
            {
                throw new DbUpdateException(
                    "Forced Employer email confirmation failure.");
            }

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; } =
            new(
                2026,
                9,
                12,
                2,
                0,
                0,
                DateTimeKind.Utc);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            FakeClock clock)
        {
            _provider = provider;
            _scope = scope;

            Context = context;
            UserManager = userManager;
            RoleManager = roleManager;
            Clock = clock;
        }

        public HireSyncDbContext Context { get; }

        public UserManager<ApplicationUser> UserManager { get; }

        public RoleManager<IdentityRole<Guid>> RoleManager { get; }

        public FakeClock Clock { get; }

        public void Dispose()
        {
            try
            {
                Context.Database.EnsureDeleted();
            }
            finally
            {
                _scope.Dispose();
                _provider.Dispose();
            }
        }
    }
}
