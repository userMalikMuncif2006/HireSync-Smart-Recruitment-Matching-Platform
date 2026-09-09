using HireSync.Domain.Enums;

namespace HireSync.Domain.Entities;

public sealed class EmailOtpChallenge
{
    private EmailOtpChallenge()
    {
    }

    public EmailOtpChallenge(
        Guid id,
        string email,
        EmailOtpPurpose purpose,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "OTP challenge id is required.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(email));
        }

        if (string.IsNullOrWhiteSpace(codeHash))
        {
            throw new ArgumentException(
                "OTP code hash is required.",
                nameof(codeHash));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc ||
            expiresAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "OTP timestamps must be UTC.");
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "OTP expiry must be later than creation time.",
                nameof(expiresAtUtc));
        }

        Id = id;
        Email = email.Trim();
        Purpose = purpose;
        CodeHash = codeHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public EmailOtpPurpose Purpose { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? ConsumedAtUtc { get; private set; }

    public int FailedAttempts { get; private set; }

    public bool IsExpired(DateTime nowUtc)
    {
        return nowUtc >= ExpiresAtUtc;
    }

    public bool IsConsumed => ConsumedAtUtc.HasValue;

    public bool HasReachedAttemptLimit(int maxAttempts)
    {
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts));
        }

        return FailedAttempts >= maxAttempts;
    }

    public void RegisterFailedAttempt(int maxAttempts)
    {
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts));
        }

        if (IsConsumed)
        {
            throw new InvalidOperationException(
                "OTP challenge has already been consumed.");
        }

        if (HasReachedAttemptLimit(maxAttempts))
        {
            throw new InvalidOperationException(
                "OTP challenge attempt limit has been reached.");
        }

        FailedAttempts++;
    }

    public void MarkConsumed(DateTime consumedAtUtc)
    {
        if (IsConsumed)
        {
            throw new InvalidOperationException(
                "OTP challenge has already been consumed.");
        }

        if (consumedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Consumed timestamp must be UTC.",
                nameof(consumedAtUtc));
        }

        ConsumedAtUtc = consumedAtUtc;
    }
}
