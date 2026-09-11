using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Identity;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Identity;

public sealed class EmployerRegistrationProvisioner
    : IEmployerRegistrationProvisioner
{
    private readonly HireSyncDbContext _dbContext;
    private readonly IIdentityService _identityService;
    private readonly IClock _clock;

    public EmployerRegistrationProvisioner(
        HireSyncDbContext dbContext,
        IIdentityService identityService,
        IClock clock)
    {
        _dbContext = dbContext;
        _identityService = identityService;
        _clock = clock;
    }

    public async Task<EmployerRegistrationResult> ProvisionAsync(
        RegisterEmployerRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
            return EmployerRegistrationResult.Failure(
                EmployerRegistrationFailureReason.InvalidInput);
        }

        var duplicateBusinessRegistrationNumber =
            await _dbContext.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(
                    profile =>
                        profile.NormalizedBusinessRegistrationNumber ==
                        normalizedBusinessRegistrationNumber,
                    cancellationToken);

        if (duplicateBusinessRegistrationNumber)
        {
            return EmployerRegistrationResult.Failure(
                EmployerRegistrationFailureReason
                    .DuplicateBusinessRegistrationNumber);
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var creation =
                await _identityService.CreateUserAsync(
                    request.Email.Trim(),
                    request.Password,
                    request.ContactPersonName.Trim(),
                    RoleNames.Employer,
                    cancellationToken);

            if (!creation.Succeeded)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return creation.FailureReason switch
                {
                    IdentityUserCreationFailure.EmailAlreadyExists =>
                        EmployerRegistrationResult.Failure(
                            EmployerRegistrationFailureReason
                                .EmailAlreadyExists),

                    _ =>
                        EmployerRegistrationResult.Failure(
                            EmployerRegistrationFailureReason
                                .IdentityValidationFailed)
                };
            }

            var nowUtc = _clock.UtcNow;

            var profile = new EmployerProfile
            {
                Id = Guid.NewGuid(),
                UserId = creation.UserId!.Value,
                CompanyName = companyName,
                NormalizedCompanyName = normalizedCompanyName,
                Description = request.Description.Trim(),
                Location = location,
                NormalizedLocation = normalizedLocation,
                ContactPersonName =
                    request.ContactPersonName.Trim(),
                ContactPersonDesignation =
                    request.ContactPersonDesignation.Trim(),
                BusinessRegistrationNumber =
                    businessRegistrationNumber,
                NormalizedBusinessRegistrationNumber =
                    normalizedBusinessRegistrationNumber,
                MobileNumber = request.MobileNumber.Trim(),
                CompanyWebsite =
                    string.IsNullOrWhiteSpace(
                        request.CompanyWebsite)
                        ? null
                        : request.CompanyWebsite.Trim(),
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };

            _dbContext.EmployerProfiles.Add(profile);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return EmployerRegistrationResult.Success(
                new RegisterEmployerResponse(
                    creation.UserId.Value,
                    profile.Id,
                    creation.Email!,
                    RoleNames.Employer,
                    EmployerVerificationStatus.Pending));
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return EmployerRegistrationResult.Failure(
                EmployerRegistrationFailureReason
                    .DuplicateBusinessRegistrationNumber);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return EmployerRegistrationResult.Failure(
                EmployerRegistrationFailureReason
                    .PersistenceFailed);
        }
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        var inner = exception.InnerException;

        while (inner is not null)
        {
            if (inner.GetType().FullName ==
                    "Microsoft.Data.SqlClient.SqlException")
            {
                var numberProperty =
                    inner.GetType().GetProperty("Number");

                if (numberProperty?.GetValue(inner) is int number &&
                    (number == 2601 || number == 2627))
                {
                    return true;
                }
            }

            inner = inner.InnerException;
        }

        return false;
    }
}
