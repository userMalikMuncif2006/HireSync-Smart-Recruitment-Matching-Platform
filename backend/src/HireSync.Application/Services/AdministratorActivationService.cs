using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Otp;
using HireSync.Application.Security;
using HireSync.Domain.Enums;

namespace HireSync.Application.Services;

public sealed class AdministratorActivationService
{
    private readonly IIdentityService _identityService;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IAdministratorActivationCompleter
        _activationCompleter;

    public AdministratorActivationService(
        IIdentityService identityService,
        IEmailOtpService emailOtpService,
        IAdministratorActivationCompleter activationCompleter)
    {
        _identityService = identityService;
        _emailOtpService = emailOtpService;
        _activationCompleter = activationCompleter;
    }

    public async Task<AdministratorActivationRequestResult>
        RequestOtpAsync(
            AdministratorActivationRequest request,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return AdministratorActivationRequestResult.Failure(
                AdministratorActivationRequestFailureReason.InvalidCredentials);
        }

        var identity =
            await _identityService.ValidateCredentialsAsync(
                request.Email.Trim(),
                request.Password,
                cancellationToken);

        if (identity is null ||
            !string.Equals(
                identity.Role,
                RoleNames.Administrator,
                StringComparison.Ordinal))
        {
            return AdministratorActivationRequestResult.Failure(
                AdministratorActivationRequestFailureReason.InvalidCredentials);
        }

        if (identity.AccountStatus == AccountStatus.Suspended)
        {
            return AdministratorActivationRequestResult.Failure(
                AdministratorActivationRequestFailureReason.Suspended);
        }

        if (identity.EmailConfirmed)
        {
            return AdministratorActivationRequestResult.Failure(
                AdministratorActivationRequestFailureReason.AlreadyActivated);
        }

        var otpResult =
            await _emailOtpService.RequestAsync(
                identity.Email,
                EmailOtpPurpose.AdministratorFirstActivation,
                cancellationToken);

        if (otpResult.Succeeded &&
            otpResult.ExpiresAtUtc.HasValue)
        {
            return AdministratorActivationRequestResult.Success(
                otpResult.ExpiresAtUtc.Value);
        }

        if (otpResult.FailureReason ==
                OtpRequestFailureReason.CooldownActive &&
            otpResult.RetryAfterSeconds.HasValue)
        {
            return AdministratorActivationRequestResult.Cooldown(
                otpResult.RetryAfterSeconds.Value);
        }

        return AdministratorActivationRequestResult.Failure(
            AdministratorActivationRequestFailureReason.OtpRequestFailed);
    }

    public async Task<AdministratorActivationVerificationResult>
        VerifyOtpAsync(
            AdministratorActivationVerifyRequest request,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return AdministratorActivationVerificationResult.Failure(
                AdministratorActivationVerificationFailureReason.InvalidCredentials);
        }

        var identity =
            await _identityService.ValidateCredentialsAsync(
                request.Email.Trim(),
                request.Password,
                cancellationToken);

        if (identity is null ||
            !string.Equals(
                identity.Role,
                RoleNames.Administrator,
                StringComparison.Ordinal))
        {
            return AdministratorActivationVerificationResult.Failure(
                AdministratorActivationVerificationFailureReason.InvalidCredentials);
        }

        if (identity.AccountStatus == AccountStatus.Suspended)
        {
            return AdministratorActivationVerificationResult.Failure(
                AdministratorActivationVerificationFailureReason.Suspended);
        }

        if (identity.EmailConfirmed)
        {
            return AdministratorActivationVerificationResult.Failure(
                AdministratorActivationVerificationFailureReason.AlreadyActivated);
        }

        var otpResult =
            await _emailOtpService.VerifyAsync(
                identity.Email,
                EmailOtpPurpose.AdministratorFirstActivation,
                request.Code,
                cancellationToken);

        if (!otpResult.Succeeded)
        {
            var reason = otpResult.FailureReason switch
            {
                OtpVerificationFailureReason.Expired =>
                    AdministratorActivationVerificationFailureReason.Expired,

                OtpVerificationFailureReason.AlreadyUsed =>
                    AdministratorActivationVerificationFailureReason.AlreadyUsed,

                OtpVerificationFailureReason.AttemptsExceeded =>
                    AdministratorActivationVerificationFailureReason.AttemptsExceeded,

                _ =>
                    AdministratorActivationVerificationFailureReason.InvalidCode
            };

            return AdministratorActivationVerificationResult.Failure(
                reason);
        }

        var activated =
            await _activationCompleter.MarkActivatedAsync(
                identity.UserId,
                cancellationToken);

        if (!activated)
        {
            return AdministratorActivationVerificationResult.Failure(
                AdministratorActivationVerificationFailureReason.PersistenceFailed);
        }

        return AdministratorActivationVerificationResult.Success();
    }
}
