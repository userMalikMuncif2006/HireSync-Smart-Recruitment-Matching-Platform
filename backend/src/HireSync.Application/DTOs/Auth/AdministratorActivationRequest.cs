namespace HireSync.Application.DTOs.Auth;

public sealed record AdministratorActivationRequest(
    string Email,
    string Password);
