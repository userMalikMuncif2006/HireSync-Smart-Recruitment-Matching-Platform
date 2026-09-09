namespace HireSync.Application.DTOs.Auth;

public sealed record RegisterJobSeekerResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string Role);
