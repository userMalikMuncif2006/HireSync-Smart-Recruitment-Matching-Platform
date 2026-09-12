using HireSync.Application.DTOs.ContactRequests;
using HireSync.Application.Interfaces.ContactRequests;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.ContactRequests;

public sealed class ContactRequestService
    : IContactRequestService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public ContactRequestService(
        HireSyncDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<ContactRequestWriteResult>
        CreateForOwnApplicationAsync(
            Guid employerUserId,
            Guid jobApplicationId,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (employerUserId == Guid.Empty ||
            jobApplicationId == Guid.Empty)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.InvalidInput);
        }

        var employer =
            await _userManager.Users
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == employerUserId,
                    cancellationToken);

        if (employer is null)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.NotFound);
        }

        if (employer.AccountStatus != AccountStatus.Active)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .ParticipantInactive);
        }

        if (!await HasExactlyRoleAsync(
                employer,
                RoleNames.Employer,
                cancellationToken))
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.NotFound);
        }

        var ownedApplication =
            await (
                from jobApplication in
                    _dbContext.JobApplications

                join vacancy in
                    _dbContext.Vacancies
                    on jobApplication.VacancyId
                    equals vacancy.Id

                join employerProfile in
                    _dbContext.EmployerProfiles
                    on vacancy.EmployerProfileId
                    equals employerProfile.Id

                join jobSeekerProfile in
                    _dbContext.JobSeekerProfiles
                    on jobApplication.JobSeekerProfileId
                    equals jobSeekerProfile.Id

                where
                    jobApplication.Id ==
                        jobApplicationId &&
                    employerProfile.UserId ==
                        employerUserId

                select new
                {
                    Application =
                        jobApplication,

                    JobSeekerUserId =
                        jobSeekerProfile.UserId
                })
            .SingleOrDefaultAsync(
                cancellationToken);

        if (ownedApplication is null)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.NotFound);
        }

        if (ownedApplication.Application.Status ==
            ApplicationStatus.Rejected)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .ApplicationRejected);
        }

        var jobSeeker =
            await _userManager.Users
                .SingleOrDefaultAsync(
                    user =>
                        user.Id ==
                        ownedApplication.JobSeekerUserId,
                    cancellationToken);

        if (jobSeeker is null)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.NotFound);
        }

        if (jobSeeker.AccountStatus != AccountStatus.Active)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .ParticipantInactive);
        }

        if (!await HasExactlyRoleAsync(
                jobSeeker,
                RoleNames.JobSeeker,
                cancellationToken))
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.NotFound);
        }

        var alreadyExists =
            await _dbContext.ContactRequests
                .AnyAsync(
                    request =>
                        request.JobApplicationId ==
                            jobApplicationId,
                    cancellationToken);

        if (alreadyExists)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .AlreadyExists);
        }

        ContactRequest contactRequest;

        try
        {
            contactRequest =
                new ContactRequest(
                    Guid.NewGuid(),
                    jobApplicationId,
                    _clock.UtcNow);
        }
        catch (ArgumentException)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .InvalidInput);
        }

        _dbContext.ContactRequests.Add(
            contactRequest);

        try
        {
            // Contact creation intentionally creates
            // no notification.
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .PersistenceFailed);
        }

        return ContactRequestWriteResult.Success(
            ToDto(contactRequest));
    }

    public async Task<ContactRequestWriteResult>
        RespondToOwnContactRequestAsync(
            Guid jobSeekerUserId,
            Guid contactRequestId,
            RespondContactRequestRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (jobSeekerUserId == Guid.Empty ||
            contactRequestId == Guid.Empty ||
            request is null ||
            !ContactRequestWriteRules.IsValid(request))
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .InvalidInput);
        }

        var jobSeeker =
            await _userManager.Users
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == jobSeekerUserId,
                    cancellationToken);

        if (jobSeeker is null)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.NotFound);
        }

        if (jobSeeker.AccountStatus != AccountStatus.Active)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .ParticipantInactive);
        }

        if (!await HasExactlyRoleAsync(
                jobSeeker,
                RoleNames.JobSeeker,
                cancellationToken))
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.NotFound);
        }

        var contactRequest =
            await (
                from candidate in
                    _dbContext.ContactRequests

                join jobApplication in
                    _dbContext.JobApplications
                    on candidate.JobApplicationId
                    equals jobApplication.Id

                join jobSeekerProfile in
                    _dbContext.JobSeekerProfiles
                    on jobApplication.JobSeekerProfileId
                    equals jobSeekerProfile.Id

                where
                    candidate.Id ==
                        contactRequestId &&
                    jobSeekerProfile.UserId ==
                        jobSeekerUserId

                select candidate)
            .SingleOrDefaultAsync(
                cancellationToken);

        if (contactRequest is null)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason.NotFound);
        }

        try
        {
            contactRequest.Respond(
                request.Status,
                _clock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .InvalidTransition);
        }
        catch (ArgumentOutOfRangeException)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .InvalidInput);
        }

        _dbContext.Entry(contactRequest)
            .Property(candidate =>
                candidate.RowVersion)
            .OriginalValue =
                request.RowVersion;

        try
        {
            // Contact response intentionally creates
            // no notification and reveals no contact data.
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return ContactRequestWriteResult.Failure(
                ContactRequestWriteFailureReason
                    .PersistenceFailed);
        }

        return ContactRequestWriteResult.Success(
            ToDto(contactRequest));
    }

    private async Task<bool> HasExactlyRoleAsync(
        ApplicationUser user,
        string expectedRole,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles =
            await _userManager.GetRolesAsync(
                user);

        cancellationToken.ThrowIfCancellationRequested();

        return roles.Count == 1 &&
               string.Equals(
                   roles[0],
                   expectedRole,
                   StringComparison.Ordinal);
    }

    private static ContactRequestDto ToDto(
        ContactRequest contactRequest)
    {
        return new ContactRequestDto(
            contactRequest.Id,
            contactRequest.JobApplicationId,
            contactRequest.Status,
            contactRequest.RequestedAtUtc,
            contactRequest.RespondedAtUtc,
            contactRequest.RowVersion);
    }
}
