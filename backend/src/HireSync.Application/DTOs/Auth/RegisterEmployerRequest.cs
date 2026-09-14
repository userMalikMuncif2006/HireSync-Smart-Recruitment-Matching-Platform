namespace HireSync.Application.DTOs.Auth;

public sealed record RegisterEmployerRequest(
    string Email,
    string Password,
    string CompanyName,
    string Description,
    string Location,
    string ContactPersonName,
    string ContactPersonDesignation,
    string BusinessRegistrationNumber,
    string MobileNumber,
    string? CompanyWebsite);
