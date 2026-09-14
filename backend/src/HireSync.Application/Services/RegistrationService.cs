using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Security;

namespace HireSync.Application.Services;

public sealed class RegistrationService
{
    private readonly IIdentityService _identityService;

    public RegistrationService(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RegistrationResult> RegisterJobSeekerAsync(
        RegisterJobSeekerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return RegistrationResult.Failure(
                RegistrationFailureReason.InvalidInput);
        }

        var creation = await _identityService.CreateUserAsync(
            request.Email.Trim(),
            request.Password,
            request.DisplayName.Trim(),
            RoleNames.JobSeeker,
            cancellationToken);

        if (!creation.Succeeded)
        {
            return creation.FailureReason switch
            {
                IdentityUserCreationFailure.EmailAlreadyExists =>
                    RegistrationResult.Failure(
                        RegistrationFailureReason.EmailAlreadyExists),

                _ => RegistrationResult.Failure(
                    RegistrationFailureReason.IdentityValidationFailed)
            };
        }

        return RegistrationResult.Success(
            new RegisterJobSeekerResponse(
                creation.UserId!.Value,
                creation.Email!,
                creation.DisplayName!,
                RoleNames.JobSeeker));
    }
}
