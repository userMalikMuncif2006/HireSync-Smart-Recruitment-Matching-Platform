using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Domain.Rules;

namespace HireSync.Application.Services;

public sealed class EmployerRegistrationService
{
    private readonly IEmployerRegistrationProvisioner _provisioner;

    public EmployerRegistrationService(
        IEmployerRegistrationProvisioner provisioner)
    {
        _provisioner = provisioner;
    }

    public Task<EmployerRegistrationResult> RegisterAsync(
        RegisterEmployerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(request))
        {
            return Task.FromResult(
                EmployerRegistrationResult.Failure(
                    EmployerRegistrationFailureReason.InvalidInput));
        }

        return _provisioner.ProvisionAsync(
            request,
            cancellationToken);
    }

    private static bool IsValid(
        RegisterEmployerRequest request)
    {
        return
            !string.IsNullOrWhiteSpace(request.Email) &&
            !string.IsNullOrWhiteSpace(request.Password) &&

            EmployerProfileRules.HasValidRequiredText(
                request.CompanyName,
                EmployerProfileRules.CompanyNameMinLength,
                EmployerProfileRules.CompanyNameMaxLength) &&

            EmployerProfileRules.HasValidRequiredText(
                request.Description,
                EmployerProfileRules.DescriptionMinLength,
                EmployerProfileRules.DescriptionMaxLength) &&

            EmployerProfileRules.HasValidRequiredText(
                request.Location,
                EmployerProfileRules.LocationMinLength,
                EmployerProfileRules.LocationMaxLength) &&

            EmployerProfileRules.HasValidRequiredText(
                request.ContactPersonName,
                EmployerProfileRules.ContactPersonNameMinLength,
                EmployerProfileRules.ContactPersonNameMaxLength) &&

            EmployerProfileRules.HasValidRequiredText(
                request.ContactPersonDesignation,
                EmployerProfileRules.ContactPersonDesignationMinLength,
                EmployerProfileRules.ContactPersonDesignationMaxLength) &&

            EmployerProfileRules.HasValidRequiredText(
                request.BusinessRegistrationNumber,
                EmployerProfileRules.BusinessRegistrationNumberMinLength,
                EmployerProfileRules.BusinessRegistrationNumberMaxLength) &&

            EmployerProfileRules.HasValidRequiredText(
                request.MobileNumber,
                EmployerProfileRules.MobileNumberMinLength,
                EmployerProfileRules.MobileNumberMaxLength) &&

            EmployerProfileRules.IsValidCompanyWebsite(
                request.CompanyWebsite);
    }
}
