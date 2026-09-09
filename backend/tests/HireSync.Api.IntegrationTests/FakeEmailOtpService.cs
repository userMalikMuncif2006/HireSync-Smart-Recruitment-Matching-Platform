using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Otp;
using HireSync.Domain.Enums;

namespace HireSync.Api.IntegrationTests;

internal sealed class FakeEmailOtpService : IEmailOtpService
{
    public OtpRequestResult RequestResult { get; set; } =
        OtpRequestResult.Success(
            new DateTime(2026, 9, 9, 10, 5, 0, DateTimeKind.Utc));

    public OtpVerificationResult VerificationResult { get; set; } =
        OtpVerificationResult.Success();

    public int RequestCallCount { get; private set; }
    public int VerificationCallCount { get; private set; }
    public string? LastRequestEmail { get; private set; }
    public EmailOtpPurpose? LastRequestPurpose { get; private set; }
    public EmailOtpPurpose? LastVerificationPurpose { get; private set; }
    public string? LastVerificationCode { get; private set; }

    public Task<OtpRequestResult> RequestAsync(
        string email,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        RequestCallCount++;
        LastRequestEmail = email;
        LastRequestPurpose = purpose;
        return Task.FromResult(RequestResult);
    }

    public Task<OtpVerificationResult> VerifyAsync(
        string email,
        EmailOtpPurpose purpose,
        string code,
        CancellationToken cancellationToken = default)
    {
        VerificationCallCount++;
        LastVerificationPurpose = purpose;
        LastVerificationCode = code;
        return Task.FromResult(VerificationResult);
    }
}
