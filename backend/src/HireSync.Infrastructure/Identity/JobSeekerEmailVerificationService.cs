using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Identity;

public sealed class JobSeekerEmailVerificationService
    : IJobSeekerEmailVerificationService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IClock _clock;

    public JobSeekerEmailVerificationService(
        HireSyncDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailOtpService emailOtpService,
        IClock clock)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailOtpService = emailOtpService;
        _clock = clock;
    }

    public async Task<JobSeekerEmailVerificationRequestResult> RequestAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return JobSeekerEmailVerificationRequestResult.Failure(
                JobSeekerEmailVerificationRequestFailureReason.InvalidRequest);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var trimmedEmail = email.Trim();

        var user =
            await _userManager.FindByEmailAsync(
                trimmedEmail);

        if (user is null)
        {
            return JobSeekerEmailVerificationRequestResult.Failure(
                JobSeekerEmailVerificationRequestFailureReason.InvalidAccount);
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        if (roles.Count != 1 ||
            !string.Equals(
                roles[0],
                RoleNames.JobSeeker,
                StringComparison.Ordinal))
        {
            return JobSeekerEmailVerificationRequestResult.Failure(
                JobSeekerEmailVerificationRequestFailureReason.InvalidAccount);
        }

        if (user.AccountStatus == AccountStatus.Suspended)
        {
            return JobSeekerEmailVerificationRequestResult.Failure(
                JobSeekerEmailVerificationRequestFailureReason.Suspended);
        }

        if (user.EmailConfirmed)
        {
            return JobSeekerEmailVerificationRequestResult.Failure(
                JobSeekerEmailVerificationRequestFailureReason.AlreadyVerified);
        }

        var otpResult =
            await _emailOtpService.RequestAsync(
                trimmedEmail,
                EmailOtpPurpose.JobSeekerRegistration,
                cancellationToken);

        if (otpResult.Succeeded &&
            otpResult.ExpiresAtUtc.HasValue)
        {
            return JobSeekerEmailVerificationRequestResult.Success(
                otpResult.ExpiresAtUtc.Value);
        }

        if (otpResult.FailureReason ==
                OtpRequestFailureReason.CooldownActive &&
            otpResult.RetryAfterSeconds.HasValue)
        {
            return JobSeekerEmailVerificationRequestResult.Cooldown(
                otpResult.RetryAfterSeconds.Value);
        }

        return JobSeekerEmailVerificationRequestResult.Failure(
            JobSeekerEmailVerificationRequestFailureReason.RequestFailed);
    }

    public async Task<JobSeekerEmailVerificationResult> VerifyAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(code))
        {
            return JobSeekerEmailVerificationResult.Failure(
                JobSeekerEmailVerificationFailureReason.InvalidRequest);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var trimmedEmail = email.Trim();

        var user =
            await _userManager.FindByEmailAsync(
                trimmedEmail);

        if (user is null)
        {
            return JobSeekerEmailVerificationResult.Failure(
                JobSeekerEmailVerificationFailureReason.InvalidAccount);
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        if (roles.Count != 1 ||
            !string.Equals(
                roles[0],
                RoleNames.JobSeeker,
                StringComparison.Ordinal))
        {
            return JobSeekerEmailVerificationResult.Failure(
                JobSeekerEmailVerificationFailureReason.InvalidAccount);
        }

        if (user.AccountStatus == AccountStatus.Suspended)
        {
            return JobSeekerEmailVerificationResult.Failure(
                JobSeekerEmailVerificationFailureReason.Suspended);
        }

        if (user.EmailConfirmed)
        {
            return JobSeekerEmailVerificationResult.Failure(
                JobSeekerEmailVerificationFailureReason.AlreadyVerified);
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var otpResult =
                await _emailOtpService.VerifyAsync(
                    trimmedEmail,
                    EmailOtpPurpose.JobSeekerRegistration,
                    code,
                    cancellationToken);

            if (!otpResult.Succeeded)
            {
                await transaction.CommitAsync(
                    cancellationToken);

                var reason =
                    otpResult.FailureReason switch
                    {
                        OtpVerificationFailureReason.Expired =>
                            JobSeekerEmailVerificationFailureReason.Expired,

                        OtpVerificationFailureReason.AlreadyUsed =>
                            JobSeekerEmailVerificationFailureReason.AlreadyUsed,

                        OtpVerificationFailureReason.AttemptsExceeded =>
                            JobSeekerEmailVerificationFailureReason.AttemptsExceeded,

                        _ =>
                            JobSeekerEmailVerificationFailureReason.InvalidCode
                    };

                return JobSeekerEmailVerificationResult.Failure(
                    reason);
            }

            user.EmailConfirmed = true;
            user.UpdatedAtUtc = _clock.UtcNow;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return JobSeekerEmailVerificationResult.Failure(
                    JobSeekerEmailVerificationFailureReason.PersistenceFailed);
            }

            await transaction.CommitAsync(
                cancellationToken);

            return JobSeekerEmailVerificationResult.Success();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return JobSeekerEmailVerificationResult.Failure(
                JobSeekerEmailVerificationFailureReason.PersistenceFailed);
        }
    }
}
