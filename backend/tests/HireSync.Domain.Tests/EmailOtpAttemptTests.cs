using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests;

public class EmailOtpAttemptTests
{
    private static EmailOtpChallenge CreateChallenge()
    {
        var createdAtUtc =
            new DateTime(
                2026,
                9,
                9,
                9,
                0,
                0,
                DateTimeKind.Utc);

        return new EmailOtpChallenge(
            Guid.NewGuid(),
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "hashed-code-value",
            createdAtUtc,
            createdAtUtc.AddMinutes(5));
    }

    [Fact]
    public void Failed_attempt_is_recorded()
    {
        var challenge = CreateChallenge();

        challenge.RegisterFailedAttempt(5);

        Assert.Equal(
            1,
            challenge.FailedAttempts);
    }

    [Fact]
    public void Attempt_limit_is_detected()
    {
        var challenge = CreateChallenge();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            challenge.RegisterFailedAttempt(5);
        }

        Assert.True(
            challenge.HasReachedAttemptLimit(5));
    }

    [Fact]
    public void Attempt_beyond_limit_is_rejected()
    {
        var challenge = CreateChallenge();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            challenge.RegisterFailedAttempt(5);
        }

        Assert.Throws<InvalidOperationException>(() =>
            challenge.RegisterFailedAttempt(5));
    }
}
