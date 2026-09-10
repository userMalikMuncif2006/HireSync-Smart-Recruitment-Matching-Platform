using HireSync.Application.DTOs.Employer;
using HireSync.Application.Interfaces.Employer;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Employer;

public sealed class EmployerProfileService : IEmployerProfileService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public EmployerProfileService(
        HireSyncDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<EmployerProfileDto?> GetOwnProfileAsync(
        Guid employerUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var profile = await _dbContext.EmployerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == employerUserId,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == employerUserId,
                cancellationToken);

        if (!await IsValidEmployerAsync(user, cancellationToken))
        {
            return null;
        }

        return ToDto(profile, user!);
    }

    public async Task<EmployerProfileUpdateResult> UpdateOwnProfileAsync(
        Guid employerUserId,
        UpdateEmployerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsValidRequest(request))
        {
            return EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason.InvalidInput);
        }

        string companyName;
        string normalizedCompanyName;
        string location;
        string normalizedLocation;
        string businessRegistrationNumber;
        string normalizedBusinessRegistrationNumber;

        try
        {
            companyName =
                CompanyNameNormalizer.CanonicalizeDisplayName(
                    request.CompanyName);

            normalizedCompanyName =
                CompanyNameNormalizer.Normalize(
                    request.CompanyName);

            location =
                LocationNormalizer.CanonicalizeDisplayLocation(
                    request.Location);

            normalizedLocation =
                LocationNormalizer.Normalize(
                    request.Location);

            businessRegistrationNumber =
                BusinessRegistrationNumberNormalizer
                    .CanonicalizeDisplayValue(
                        request.BusinessRegistrationNumber);

            normalizedBusinessRegistrationNumber =
                BusinessRegistrationNumberNormalizer.Normalize(
                    request.BusinessRegistrationNumber);
        }
        catch (ArgumentException)
        {
            return EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason.InvalidInput);
        }

        var profile = await _dbContext.EmployerProfiles
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == employerUserId,
                cancellationToken);

        if (profile is null)
        {
            return EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason.NotFound);
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == employerUserId,
                cancellationToken);

        if (!await IsValidEmployerAsync(user, cancellationToken))
        {
            return EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason.NotFound);
        }

        var duplicateBusinessRegistrationNumber =
            await _dbContext.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(
                    candidate =>
                        candidate.Id != profile.Id &&
                        candidate.NormalizedBusinessRegistrationNumber ==
                            normalizedBusinessRegistrationNumber,
                    cancellationToken);

        if (duplicateBusinessRegistrationNumber)
        {
            return EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason
                    .DuplicateBusinessRegistrationNumber);
        }

        var companyIdentityChanged =
            !string.Equals(
                profile.NormalizedCompanyName,
                normalizedCompanyName,
                StringComparison.Ordinal) ||
            !string.Equals(
                profile.NormalizedBusinessRegistrationNumber,
                normalizedBusinessRegistrationNumber,
                StringComparison.Ordinal);

        if (companyIdentityChanged &&
            user!.EmployerVerificationStatus ==
                EmployerVerificationStatus.Approved)
        {
            user.EmployerVerificationStatus =
                EmployerVerificationStatus.Pending;

            user.UpdatedAtUtc = _clock.UtcNow;

            var resetResult =
                await _userManager.UpdateAsync(user);

            if (!resetResult.Succeeded)
            {
                return EmployerProfileUpdateResult.Failure(
                    EmployerProfileUpdateFailureReason.PersistenceFailed);
            }

            return EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason
                    .VerificationResetRequired);
        }

        profile.CompanyName = companyName;
        profile.NormalizedCompanyName = normalizedCompanyName;

        profile.Description = request.Description.Trim();

        profile.Location = location;
        profile.NormalizedLocation = normalizedLocation;

        profile.ContactPersonName =
            request.ContactPersonName.Trim();

        profile.ContactPersonDesignation =
            request.ContactPersonDesignation.Trim();

        profile.BusinessRegistrationNumber =
            businessRegistrationNumber;

        profile.NormalizedBusinessRegistrationNumber =
            normalizedBusinessRegistrationNumber;

        profile.MobileNumber =
            request.MobileNumber.Trim();

        profile.CompanyWebsite =
            string.IsNullOrWhiteSpace(request.CompanyWebsite)
                ? null
                : request.CompanyWebsite.Trim();

        profile.UpdatedAtUtc = _clock.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            return EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason
                    .DuplicateBusinessRegistrationNumber);
        }
        catch (DbUpdateException)
        {
            return EmployerProfileUpdateResult.Failure(
                EmployerProfileUpdateFailureReason.PersistenceFailed);
        }

        return EmployerProfileUpdateResult.Success(
            ToDto(profile, user!));
    }

    private async Task<bool> IsValidEmployerAsync(
        ApplicationUser? user,
        CancellationToken cancellationToken)
    {
        if (user is null ||
            string.IsNullOrWhiteSpace(user.Email) ||
            !user.EmployerVerificationStatus.HasValue)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var roles =
            await _userManager.GetRolesAsync(user);

        cancellationToken.ThrowIfCancellationRequested();

        return roles.Count == 1 &&
               string.Equals(
                   roles[0],
                   RoleNames.Employer,
                   StringComparison.Ordinal);
    }

    private static bool IsValidRequest(
        UpdateEmployerProfileRequest request)
    {
        return EmployerProfileRules.HasValidRequiredText(
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
                   EmployerProfileRules
                       .BusinessRegistrationNumberMinLength,
                   EmployerProfileRules
                       .BusinessRegistrationNumberMaxLength) &&
               EmployerProfileRules.HasValidRequiredText(
                   request.MobileNumber,
                   EmployerProfileRules.MobileNumberMinLength,
                   EmployerProfileRules.MobileNumberMaxLength) &&
               EmployerProfileRules.IsValidCompanyWebsite(
                   request.CompanyWebsite);
    }

    private static EmployerProfileDto ToDto(
        EmployerProfile profile,
        ApplicationUser user)
    {
        var verificationStatus =
            user.EmployerVerificationStatus!.Value;

        return new EmployerProfileDto(
            profile.Id,
            profile.CompanyName,
            profile.Description,
            profile.Location,
            profile.ContactPersonName,
            profile.ContactPersonDesignation,
            profile.BusinessRegistrationNumber,
            profile.MobileNumber,
            profile.CompanyWebsite,
            user.Email!,
            verificationStatus,
            profile.IsProfileComplete,
            EmployerVacancyReadinessRules.IsVacancyReady(
                user.AccountStatus,
                verificationStatus,
                profile.IsProfileComplete));
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               sqlException.Number is 2601 or 2627;
    }
}