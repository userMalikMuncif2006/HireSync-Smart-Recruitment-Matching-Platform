using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Interfaces.Time;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace HireSync.Infrastructure.Identity;

public sealed class PasswordResetService
    : IPasswordResetService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IClock _clock;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        HireSyncDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailOtpService emailOtpService,
        IClock clock,
        ILogger<PasswordResetService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailOtpService = emailOtpService;
        _clock = clock;
        _logger = logger;
    }

    public async Task<bool> RequestAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user =
                await _userManager.FindByEmailAsync(
                    email.Trim());

            if (user is null ||
                string.IsNullOrWhiteSpace(user.Email))
            {
                return true;
            }

            await _emailOtpService.RequestAsync(
                user.Email,
                EmailOtpPurpose.PasswordReset,
                cancellationToken);

            // Deliberately do not expose whether:
            // - an account exists,
            // - an OTP was sent,
            // - a resend cooldown is active.
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Preserve the anti-enumeration contract.
            // Do not log the submitted email address.
            _logger.LogWarning(
                exception,
                "Password reset OTP request could not be completed.");

            return true;
        }
    }

    public async Task<PasswordResetCompleteResult> CompleteAsync(
        PasswordResetCompleteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return PasswordResetCompleteResult.Failure(
                PasswordResetCompleteFailureReason.InvalidRequest);
        }

        IDbContextTransaction? transaction = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            var normalizedEmail =
                request.Email.Trim();

            var otpResult =
                await _emailOtpService.VerifyAsync(
                    normalizedEmail,
                    EmailOtpPurpose.PasswordReset,
                    request.Code,
                    cancellationToken);

            if (!otpResult.Succeeded)
            {
                // Invalid-code attempts must remain persisted.
                await transaction.CommitAsync(
                    cancellationToken);

                return PasswordResetCompleteResult.Failure(
                    PasswordResetCompleteFailureReason.InvalidOrExpiredCode);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var user =
                await _userManager.FindByEmailAsync(
                    normalizedEmail);

            if (user is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _dbContext.ChangeTracker.Clear();

                return PasswordResetCompleteResult.Failure(
                    PasswordResetCompleteFailureReason.InvalidOrExpiredCode);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var resetToken =
                await _userManager
                    .GeneratePasswordResetTokenAsync(
                        user);

            var passwordResult =
                await _userManager.ResetPasswordAsync(
                    user,
                    resetToken,
                    request.NewPassword);

            if (!passwordResult.Succeeded)
            {
                var passwordErrors =
                    passwordResult.Errors
                        .Select(error =>
                            error.Description)
                        .Where(description =>
                            !string.IsNullOrWhiteSpace(
                                description))
                        .Distinct(
                            StringComparer.Ordinal)
                        .ToArray();

                await transaction.RollbackAsync(
                    cancellationToken);

                _dbContext.ChangeTracker.Clear();

                return PasswordResetCompleteResult.InvalidPassword(
                    passwordErrors);
            }

            user.TokenVersion =
                checked(
                    user.TokenVersion + 1);

            user.UpdatedAtUtc =
                _clock.UtcNow;

            var updateResult =
                await _userManager.UpdateAsync(
                    user);

            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _dbContext.ChangeTracker.Clear();

                return PasswordResetCompleteResult.Failure(
                    PasswordResetCompleteFailureReason.PersistenceFailed);
            }

            await transaction.CommitAsync(
                cancellationToken);

            return PasswordResetCompleteResult.Success();
        }
        catch (OperationCanceledException)
        {
            if (transaction is not null)
            {
                try
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);
                }
                catch
                {
                    // Preserve the original cancellation.
                }
            }

            _dbContext.ChangeTracker.Clear();

            throw;
        }
        catch (Exception exception)
        {
            if (transaction is not null)
            {
                try
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);
                }
                catch
                {
                    // Preserve the original failure result.
                }
            }

            _dbContext.ChangeTracker.Clear();

            _logger.LogError(
                exception,
                "Password reset completion failed.");

            return PasswordResetCompleteResult.Failure(
                PasswordResetCompleteFailureReason.PersistenceFailed);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
