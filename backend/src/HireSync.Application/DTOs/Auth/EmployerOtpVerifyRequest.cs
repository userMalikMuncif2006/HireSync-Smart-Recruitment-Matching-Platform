namespace HireSync.Application.DTOs.Auth;

public sealed record EmployerOtpVerifyRequest(
    string Email,
    string Code);
