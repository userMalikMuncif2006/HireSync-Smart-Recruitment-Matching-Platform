using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests;

public class EmailOtpChallengeTests
{
    [Fact]
    public void Challenge_stores_only_code_hash_contract()
    {
        var createdAtUtc =
            new DateTime(2026, 9, 9, 1, 30, 0, DateTimeKind.Utc);

        var challenge = new EmailOtpChallenge(
            Guid.NewGuid(),
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "hashed-code-value",
            createdAtUtc,
            createdAtUtc.AddMinutes(1));

        Assert.Equal(
            "hashed-code-value",
            challenge.CodeHash);

        Assert.False(challenge.IsConsumed);
    }

    [Fact]
    public void Challenge_reports_expired_at_expiry_boundary()
    {
        var createdAtUtc =
            new DateTime(2026, 9, 9, 1, 30, 0, DateTimeKind.Utc);

        var expiresAtUtc = createdAtUtc.AddMinutes(1);

        var challenge = new EmailOtpChallenge(
            Guid.NewGuid(),
            "employer@example.com",
            EmailOtpPurpose.EmployerRegistration,
            "hashed-code-value",
            createdAtUtc,
            expiresAtUtc);

        Assert.True(
            challenge.IsExpired(expiresAtUtc));
    }

    [Fact]
    public void Challenge_can_be_consumed_only_once()
    {
        var createdAtUtc =
            new DateTime(2026, 9, 9, 1, 30, 0, DateTimeKind.Utc);

        var challenge = new EmailOtpChallenge(
            Guid.NewGuid(),
            "admin@example.com",
            EmailOtpPurpose.AdministratorFirstActivation,
            "hashed-code-value",
            createdAtUtc,
            createdAtUtc.AddMinutes(1));

        challenge.MarkConsumed(
            createdAtUtc.AddSeconds(30));

        Assert.True(challenge.IsConsumed);

        Assert.Throws<InvalidOperationException>(() =>
            challenge.MarkConsumed(
                createdAtUtc.AddSeconds(40)));
    }

    [Fact]
    public void Challenge_requires_expiry_after_creation()
    {
        var createdAtUtc =
            new DateTime(2026, 9, 9, 1, 30, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() =>
            new EmailOtpChallenge(
                Guid.NewGuid(),
                "employer@example.com",
                EmailOtpPurpose.EmployerRegistration,
                "hashed-code-value",
                createdAtUtc,
                createdAtUtc));
    }
}
