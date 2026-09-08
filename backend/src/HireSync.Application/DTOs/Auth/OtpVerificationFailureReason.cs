namespace HireSync.Application.DTOs.Auth;

public enum OtpVerificationFailureReason
{
    InvalidCode = 1,
    Expired = 2,
    AlreadyUsed = 3
}
