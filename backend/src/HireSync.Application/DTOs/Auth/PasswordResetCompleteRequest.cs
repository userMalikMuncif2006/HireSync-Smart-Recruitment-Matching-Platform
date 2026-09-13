namespace HireSync.Application.DTOs.Auth;

public sealed record PasswordResetCompleteRequest(
    string Email,
    string Code,
    string NewPassword);
