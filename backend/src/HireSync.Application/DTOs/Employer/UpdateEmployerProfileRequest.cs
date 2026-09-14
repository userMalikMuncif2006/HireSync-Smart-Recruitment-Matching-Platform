namespace HireSync.Application.DTOs.Employer;

public sealed record UpdateEmployerProfileRequest(
    string CompanyName,
    string Description,
    string Location,
    string ContactPersonName,
    string ContactPersonDesignation,
    string BusinessRegistrationNumber,
    string MobileNumber,
    string? CompanyWebsite);