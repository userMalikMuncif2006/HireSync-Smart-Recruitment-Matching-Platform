namespace HireSync.Application.DTOs.Auth;

public sealed record AdministratorActivationVerifyRequest(
    string Email,
    string Password,
    string Code);
