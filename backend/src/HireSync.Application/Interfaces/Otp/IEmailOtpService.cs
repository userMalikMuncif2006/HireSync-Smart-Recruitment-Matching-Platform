using HireSync.Application.DTOs.Auth;
using HireSync.Domain.Enums;

namespace HireSync.Application.Interfaces.Otp;

public interface IEmailOtpService
{
    Task<OtpRequestResult> RequestAsync(
        string email,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default);

    Task<OtpVerificationResult> VerifyAsync(
        string email,
        EmailOtpPurpose purpose,
        string code,
        CancellationToken cancellationToken = default);
}
