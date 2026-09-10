using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Email;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Application.Services;

public sealed class EmailOtpService : IEmailOtpService
{
    private readonly IEmailOtpChallengeStore _store;
    private readonly IEmailOtpCodeGenerator _generator;
    private readonly IEmailOtpCodeHasher _hasher;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;

    public EmailOtpService(
        IEmailOtpChallengeStore store,
        IEmailOtpCodeGenerator generator,
        IEmailOtpCodeHasher hasher,
        IEmailSender emailSender,
        IClock clock)
    {
        _store = store;
        _generator = generator;
        _hasher = hasher;
        _emailSender = emailSender;
        _clock = clock;
    }

    public async Task<OtpRequestResult> RequestAsync(
        string email,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(email));
        }

        var trimmedEmail = email.Trim();
        var nowUtc = _clock.UtcNow;

        var latest = await _store.GetLatestAsync(
            trimmedEmail,
            purpose,
            cancellationToken);

        if (latest is not null)
        {
            var cooldownEndsAtUtc =
                latest.CreatedAtUtc +
                EmailOtpPolicy.ResendCooldown;

            if (nowUtc < cooldownEndsAtUtc)
            {
                var remaining =
                    cooldownEndsAtUtc - nowUtc;

                var retryAfterSeconds =
                    (int)Math.Ceiling(
                        remaining.TotalSeconds);

                return OtpRequestResult.Cooldown(
                    retryAfterSeconds);
            }
        }

        var challengeId = Guid.NewGuid();
        var code = _generator.Generate();

        var codeHash =
            _hasher.Hash(
                challengeId,
                code);

        var expiresAtUtc =
            nowUtc + EmailOtpPolicy.Lifetime;

        var challenge =
            new EmailOtpChallenge(
                challengeId,
                trimmedEmail,
                purpose,
                codeHash,
                nowUtc,
                expiresAtUtc);

        await _store.AddAsync(
            challenge,
            cancellationToken);

        await _store.SaveChangesAsync(
            cancellationToken);

        await _emailSender.SendAsync(
            trimmedEmail,
            "HireSync verification code",
            $"Your HireSync verification code is {code}. " +
            $"It expires in {(int)EmailOtpPolicy.Lifetime.TotalMinutes} minutes. " +
            "If you did not request this code, you can ignore this email.",
            cancellationToken);

        return OtpRequestResult.Success(
            expiresAtUtc);
    }

    public async Task<OtpVerificationResult> VerifyAsync(
        string email,
        EmailOtpPurpose purpose,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(code))
        {
            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.InvalidCode);
        }

        var latest =
            await _store.GetLatestAsync(
                email.Trim(),
                purpose,
                cancellationToken);

        if (latest is null)
        {
            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.InvalidCode);
        }

        if (latest.IsConsumed)
        {
            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.AlreadyUsed);
        }

        var nowUtc = _clock.UtcNow;

        if (latest.IsExpired(nowUtc))
        {
            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.Expired);
        }

        if (latest.HasReachedAttemptLimit(
            EmailOtpPolicy.MaxFailedAttempts))
        {
            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.AttemptsExceeded);
        }

        if (!_hasher.Verify(
            latest.Id,
            code,
            latest.CodeHash))
        {
            latest.RegisterFailedAttempt(
                EmailOtpPolicy.MaxFailedAttempts);

            await _store.SaveChangesAsync(
                cancellationToken);

            if (latest.HasReachedAttemptLimit(
                EmailOtpPolicy.MaxFailedAttempts))
            {
                return OtpVerificationResult.Failure(
                    OtpVerificationFailureReason.AttemptsExceeded);
            }

            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.InvalidCode);
        }

        latest.MarkConsumed(nowUtc);

        await _store.SaveChangesAsync(
            cancellationToken);

        return OtpVerificationResult.Success();
    }
}
