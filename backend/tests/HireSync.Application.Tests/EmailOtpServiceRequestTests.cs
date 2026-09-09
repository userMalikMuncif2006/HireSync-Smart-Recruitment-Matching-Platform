using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Email;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Application.Services;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Application.Tests;

public class EmailOtpServiceRequestTests
{
    [Fact]
    public async Task Request_creates_hashed_challenge_and_sends_email()
    {
        var nowUtc =
            new DateTime(
                2026,
                9,
                9,
                9,
                0,
                0,
                DateTimeKind.Utc);

        var store = new FakeStore();
        var generator = new FakeGenerator("123456");
        var hasher = new FakeHasher();
        var emailSender = new FakeEmailSender();
        var clock = new FakeClock(nowUtc);

        var service = new EmailOtpService(
            store,
            generator,
            hasher,
            emailSender,
            clock);

        var result = await service.RequestAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration);

        Assert.True(result.Succeeded);

        Assert.NotNull(store.Added);

        Assert.Equal(
            "employer@example.com",
            store.Added!.Email);

        Assert.Equal(
            EmailOtpPurpose.EmployerRegistration,
            store.Added.Purpose);

        Assert.Equal(
            "hashed-code",
            store.Added.CodeHash);

        Assert.NotEqual(
            "123456",
            store.Added.CodeHash);

        Assert.Equal(
            nowUtc + EmailOtpPolicy.Lifetime,
            store.Added.ExpiresAtUtc);

        Assert.Equal(1, store.SaveCount);

        Assert.Equal(
            "employer@example.com",
            emailSender.LastRecipient);

        Assert.Contains(
            "123456",
            emailSender.LastBody);
    }

    [Fact]
    public async Task Request_within_cooldown_is_rejected()
    {
        var nowUtc =
            new DateTime(
                2026,
                9,
                9,
                9,
                0,
                30,
                DateTimeKind.Utc);

        var existingCreatedAtUtc =
            nowUtc.AddSeconds(-30);

        var store = new FakeStore
        {
            Latest = CreateChallenge(
                existingCreatedAtUtc)
        };

        var emailSender = new FakeEmailSender();

        var service = new EmailOtpService(
            store,
            new FakeGenerator("123456"),
            new FakeHasher(),
            emailSender,
            new FakeClock(nowUtc));

        var result = await service.RequestAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration);

        Assert.False(result.Succeeded);

        Assert.Equal(
            OtpRequestFailureReason.CooldownActive,
            result.FailureReason);

        Assert.Equal(30, result.RetryAfterSeconds);

        Assert.Null(store.Added);
        Assert.Equal(0, store.SaveCount);
        Assert.Null(emailSender.LastRecipient);
    }

    [Fact]
    public async Task Request_at_cooldown_boundary_is_allowed()
    {
        var nowUtc =
            new DateTime(
                2026,
                9,
                9,
                9,
                1,
                0,
                DateTimeKind.Utc);

        var store = new FakeStore
        {
            Latest = CreateChallenge(
                nowUtc - EmailOtpPolicy.ResendCooldown)
        };

        var emailSender = new FakeEmailSender();

        var service = new EmailOtpService(
            store,
            new FakeGenerator("654321"),
            new FakeHasher(),
            emailSender,
            new FakeClock(nowUtc));

        var result = await service.RequestAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration);

        Assert.True(result.Succeeded);
        Assert.NotNull(store.Added);
        Assert.Equal(1, store.SaveCount);
        Assert.NotNull(emailSender.LastRecipient);
    }

    private static EmailOtpChallenge CreateChallenge(
        DateTime createdAtUtc)
    {
        return new EmailOtpChallenge(
            Guid.NewGuid(),
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "hashed-code",
            createdAtUtc,
            createdAtUtc + EmailOtpPolicy.Lifetime);
    }

    private sealed class FakeStore
        : IEmailOtpChallengeStore
    {
        public EmailOtpChallenge? Latest { get; init; }

        public EmailOtpChallenge? Added { get; private set; }

        public int SaveCount { get; private set; }

        public Task AddAsync(
            EmailOtpChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            Added = challenge;

            return Task.CompletedTask;
        }

        public Task<EmailOtpChallenge?> GetLatestAsync(
            string email,
            EmailOtpPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Latest);
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(1);
        }
    }

    private sealed class FakeGenerator
        : IEmailOtpCodeGenerator
    {
        private readonly string _code;

        public FakeGenerator(string code)
        {
            _code = code;
        }

        public string Generate()
        {
            return _code;
        }
    }

    private sealed class FakeHasher
        : IEmailOtpCodeHasher
    {
        public string Hash(
            Guid challengeId,
            string code)
        {
            return "hashed-code";
        }

        public bool Verify(
            Guid challengeId,
            string code,
            string expectedHash)
        {
            return code == "123456" &&
                   expectedHash == "hashed-code";
        }
    }

    private sealed class FakeEmailSender
        : IEmailSender
    {
        public string? LastRecipient { get; private set; }

        public string LastBody { get; private set; } =
            string.Empty;

        public Task SendAsync(
            string recipientEmail,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            LastRecipient = recipientEmail;
            LastBody = body;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock
        : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
