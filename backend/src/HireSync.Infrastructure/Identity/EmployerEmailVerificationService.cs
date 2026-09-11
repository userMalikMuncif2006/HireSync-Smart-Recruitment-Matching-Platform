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

public sealed class EmployerEmailVerificationService
    : IEmployerEmailVerificationService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IClock _clock;

    public EmployerEmailVerificationService(
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

    public async Task<EmployerEmailVerificationResult> VerifyAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(code))
        {
            return EmployerEmailVerificationResult.Failure(
                EmployerEmailVerificationFailureReason.InvalidRequest);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var trimmedEmail = email.Trim();

        var user =
            await _userManager.FindByEmailAsync(
                trimmedEmail);

        if (user is null)
        {
            return EmployerEmailVerificationResult.Failure(
                EmployerEmailVerificationFailureReason.InvalidAccount);
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        if (roles.Count != 1 ||
            !string.Equals(
                roles[0],
                RoleNames.Employer,
                StringComparison.Ordinal))
        {
            return EmployerEmailVerificationResult.Failure(
                EmployerEmailVerificationFailureReason.InvalidAccount);
        }

        if (user.AccountStatus == AccountStatus.Suspended)
        {
            return EmployerEmailVerificationResult.Failure(
                EmployerEmailVerificationFailureReason.Suspended);
        }

        if (user.EmailConfirmed)
        {
            return EmployerEmailVerificationResult.Failure(
                EmployerEmailVerificationFailureReason.AlreadyVerified);
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var otpResult =
                await _emailOtpService.VerifyAsync(
                    trimmedEmail,
                    EmailOtpPurpose.EmployerRegistration,
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
                            EmployerEmailVerificationFailureReason.Expired,

                        OtpVerificationFailureReason.AlreadyUsed =>
                            EmployerEmailVerificationFailureReason.AlreadyUsed,

                        OtpVerificationFailureReason.AttemptsExceeded =>
                            EmployerEmailVerificationFailureReason.AttemptsExceeded,

                        _ =>
                            EmployerEmailVerificationFailureReason.InvalidCode
                    };

                return EmployerEmailVerificationResult.Failure(
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

                return EmployerEmailVerificationResult.Failure(
                    EmployerEmailVerificationFailureReason.PersistenceFailed);
            }

            await transaction.CommitAsync(
                cancellationToken);

            return EmployerEmailVerificationResult.Success();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return EmployerEmailVerificationResult.Failure(
                EmployerEmailVerificationFailureReason.PersistenceFailed);
        }
    }
}
