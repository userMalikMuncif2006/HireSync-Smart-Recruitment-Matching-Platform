namespace HireSync.Application.DTOs.Auth;

public sealed record OtpVerificationResult(
    bool Succeeded,
    OtpVerificationFailureReason? FailureReason)
{
    public static OtpVerificationResult Success()
    {
        return new(true, null);
    }

    public static OtpVerificationResult Failure(
        OtpVerificationFailureReason reason)
    {
        return new(false, reason);
    }
}
