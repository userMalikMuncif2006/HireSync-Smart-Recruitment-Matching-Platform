namespace HireSync.Application.DTOs.Auth;

public sealed record JobSeekerOtpVerifyRequest(
    string Email,
    string Code);
