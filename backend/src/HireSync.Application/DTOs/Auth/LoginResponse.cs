namespace HireSync.Application.DTOs.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string Role);
