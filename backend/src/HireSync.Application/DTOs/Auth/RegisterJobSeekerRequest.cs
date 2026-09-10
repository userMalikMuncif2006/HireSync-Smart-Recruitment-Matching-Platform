namespace HireSync.Application.DTOs.Auth;

public sealed record RegisterJobSeekerRequest(
    string Email,
    string Password,
    string DisplayName);
