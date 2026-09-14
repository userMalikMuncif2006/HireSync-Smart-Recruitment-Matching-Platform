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

public class EmailOtpServiceVerificationTests
{
    [Fact]
    public async Task Correct_code_succeeds_and_consumes_challenge()
    {
        var nowUtc = Utc(9, 5);

        var challenge = CreateChallenge(
            nowUtc.AddMinutes(-1),
            "hash:123456");

        var store = new FakeStore(challenge);

        var service = CreateService(
            store,
            nowUtc);

        var result = await service.VerifyAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "123456");

        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);

        Assert.True(challenge.IsConsumed);
        Assert.Equal(nowUtc, challenge.ConsumedAtUtc);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Wrong_code_registers_failed_attempt()
    {
        var nowUtc = Utc(9, 5);

        var challenge = CreateChallenge(
            nowUtc.AddMinutes(-1),
            "hash:123456");

        var store = new FakeStore(challenge);

        var service = CreateService(
            store,
            nowUtc);

        var result = await service.VerifyAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "654321");

        Assert.False(result.Succeeded);

        Assert.Equal(
            OtpVerificationFailureReason.InvalidCode,
            result.FailureReason);

        Assert.Equal(1, challenge.FailedAttempts);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Fifth_wrong_attempt_reaches_attempt_limit()
    {
        var nowUtc = Utc(9, 5);

        var challenge = CreateChallenge(
            nowUtc.AddMinutes(-1),
            "hash:123456");

        for (var attempt = 0; attempt < 4; attempt++)
        {
            challenge.RegisterFailedAttempt(
                EmailOtpPolicy.MaxFailedAttempts);
        }

        var store = new FakeStore(challenge);

        var service = CreateService(
            store,
            nowUtc);

        var result = await service.VerifyAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "654321");

        Assert.False(result.Succeeded);

        Assert.Equal(
            OtpVerificationFailureReason.AttemptsExceeded,
            result.FailureReason);

        Assert.Equal(
            EmailOtpPolicy.MaxFailedAttempts,
            challenge.FailedAttempts);

        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Attempt_after_limit_is_rejected_without_mutation()
    {
        var nowUtc = Utc(9, 5);

        var challenge = CreateChallenge(
            nowUtc.AddMinutes(-1),
            "hash:123456");

        for (var attempt = 0;
             attempt < EmailOtpPolicy.MaxFailedAttempts;
             attempt++)
        {
            challenge.RegisterFailedAttempt(
                EmailOtpPolicy.MaxFailedAttempts);
        }

        var store = new FakeStore(challenge);

        var service = CreateService(
            store,
            nowUtc);

        var result = await service.VerifyAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "123456");

        Assert.False(result.Succeeded);

        Assert.Equal(
            OtpVerificationFailureReason.AttemptsExceeded,
            result.FailureReason);

        Assert.False(challenge.IsConsumed);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Expired_code_is_rejected()
    {
        var nowUtc = Utc(9, 5);

        var challenge = CreateChallenge(
            nowUtc - EmailOtpPolicy.Lifetime,
            "hash:123456");

        var store = new FakeStore(challenge);

        var service = CreateService(
            store,
            nowUtc);

        var result = await service.VerifyAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "123456");

        Assert.False(result.Succeeded);

        Assert.Equal(
            OtpVerificationFailureReason.Expired,
            result.FailureReason);

        Assert.False(challenge.IsConsumed);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Consumed_code_cannot_be_reused()
    {
        var nowUtc = Utc(9, 5);

        var challenge = CreateChallenge(
            nowUtc.AddMinutes(-1),
            "hash:123456");

        challenge.MarkConsumed(
            nowUtc.AddSeconds(-10));

        var store = new FakeStore(challenge);

        var service = CreateService(
            store,
            nowUtc);

        var result = await service.VerifyAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "123456");

        Assert.False(result.Succeeded);

        Assert.Equal(
            OtpVerificationFailureReason.AlreadyUsed,
            result.FailureReason);

        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Older_code_does_not_replace_latest_challenge()
    {
        var nowUtc = Utc(9, 5);

        var latestChallenge = CreateChallenge(
            nowUtc.AddMinutes(-1),
            "hash:222222");

        var store = new FakeStore(
            latestChallenge);

        var service = CreateService(
            store,
            nowUtc);

        var result = await service.VerifyAsync(
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "111111");

        Assert.False(result.Succeeded);

        Assert.Equal(
            OtpVerificationFailureReason.InvalidCode,
            result.FailureReason);

        Assert.Equal(
            1,
            latestChallenge.FailedAttempts);
    }

    private static EmailOtpService CreateService(
        FakeStore store,
        DateTime nowUtc)
    {
        return new EmailOtpService(
            store,
            new FakeGenerator(),
            new FakeHasher(),
            new FakeEmailSender(),
            new FakeClock(nowUtc));
    }

    private static EmailOtpChallenge CreateChallenge(
        DateTime createdAtUtc,
        string hash)
    {
        return new EmailOtpChallenge(
            Guid.NewGuid(),
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            hash,
            createdAtUtc,
            createdAtUtc + EmailOtpPolicy.Lifetime);
    }

    private static DateTime Utc(
        int hour,
        int minute)
    {
        return new DateTime(
            2026,
            9,
            9,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }

    private sealed class FakeStore
        : IEmailOtpChallengeStore
    {
        private readonly EmailOtpChallenge? _latest;

        public FakeStore(
            EmailOtpChallenge? latest)
        {
            _latest = latest;
        }

        public int SaveCount { get; private set; }

        public Task AddAsync(
            EmailOtpChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<EmailOtpChallenge?> GetLatestAsync(
            string email,
            EmailOtpPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_latest);
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;

            return Task.FromResult(1);
        }
    }

    private sealed class FakeHasher
        : IEmailOtpCodeHasher
    {
        public string Hash(
            Guid challengeId,
            string code)
        {
            return $"hash:{code}";
        }

        public bool Verify(
            Guid challengeId,
            string code,
            string expectedHash)
        {
            return expectedHash ==
                   $"hash:{code}";
        }
    }

    private sealed class FakeGenerator
        : IEmailOtpCodeGenerator
    {
        public string Generate()
        {
            return "123456";
        }
    }

    private sealed class FakeEmailSender
        : IEmailSender
    {
        public Task SendAsync(
            string recipientEmail,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock
        : IClock
    {
        public FakeClock(
            DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
