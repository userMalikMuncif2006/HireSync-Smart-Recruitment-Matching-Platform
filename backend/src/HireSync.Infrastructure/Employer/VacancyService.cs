using HireSync.Application.DTOs;
using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Employer;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Rules;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Employer;

public sealed class VacancyService : IVacancyService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClock _clock;

    public VacancyService(
        HireSyncDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IClock clock)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _clock = clock;
    }

    public async Task<VacancyCreateResult> CreateOwnVacancyAsync(
        Guid employerUserId,
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (employerUserId == Guid.Empty ||
            !IsValidCreateRequest(request))
        {
            return VacancyCreateResult.Failure(
                VacancyCreateFailureReason.InvalidInput);
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == employerUserId,
                cancellationToken);

        if (!await IsValidEmployerAsync(
                user,
                cancellationToken))
        {
            return VacancyCreateResult.Failure(
                VacancyCreateFailureReason.EmployerNotReady);
        }

        var employerProfile = await _dbContext.EmployerProfiles
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == employerUserId,
                cancellationToken);

        if (employerProfile is null ||
            !EmployerVacancyReadinessRules.IsVacancyReady(
                user!.AccountStatus,
                user.EmployerVerificationStatus,
                employerProfile.IsProfileComplete))
        {
            return VacancyCreateResult.Failure(
                VacancyCreateFailureReason.EmployerNotReady);
        }

        var requiredSkillIds =
            request.RequiredSkillIds.ToArray();

        var skills = await _dbContext.Skills
            .AsNoTracking()
            .Where(skill =>
                requiredSkillIds.Contains(skill.Id))
            .OrderBy(skill => skill.NormalizedName)
            .ThenBy(skill => skill.Id)
            .ToListAsync(cancellationToken);

        if (skills.Count != requiredSkillIds.Length)
        {
            return VacancyCreateResult.Failure(
                VacancyCreateFailureReason.InvalidRequiredSkills);
        }

        Vacancy vacancy;

        try
        {
            vacancy = new Vacancy(
                Guid.NewGuid(),
                employerProfile.Id,
                request.Title,
                request.Description,
                request.Location,
                request.MinimumExperienceMonths,
                request.RequiredEducationLevel,
                _clock.UtcNow);
        }
        catch (ArgumentException)
        {
            return VacancyCreateResult.Failure(
                VacancyCreateFailureReason.InvalidInput);
        }


        _dbContext.Vacancies.Add(vacancy);

        foreach (var skillId in requiredSkillIds)
        {
            _dbContext.VacancySkills.Add(
                new VacancySkill(
                    vacancy.Id,
                    skillId));
        }

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            return VacancyCreateResult.Failure(
                VacancyCreateFailureReason.PersistenceFailed);
        }

        return VacancyCreateResult.Success(
            ToVacancyDto(
                vacancy,
                skills));
    }

    public async Task<VacancyStatusUpdateResult>
        UpdateOwnVacancyStatusAsync(
            Guid employerUserId,
            Guid vacancyId,
            UpdateVacancyStatusRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (employerUserId == Guid.Empty ||
            vacancyId == Guid.Empty ||
            !VacancyStatusUpdateRules.IsValid(request))
        {
            return VacancyStatusUpdateResult.Failure(
                VacancyStatusUpdateFailureReason.InvalidInput);
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Id == employerUserId,
                cancellationToken);

        if (!await IsValidEmployerAsync(
                user,
                cancellationToken))
        {
            return VacancyStatusUpdateResult.Failure(
                VacancyStatusUpdateFailureReason.NotFound);
        }

        var employerProfile = await _dbContext.EmployerProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.UserId == employerUserId,
                cancellationToken);

        if (employerProfile is null)
        {
            return VacancyStatusUpdateResult.Failure(
                VacancyStatusUpdateFailureReason.NotFound);
        }

        var vacancy = await _dbContext.Vacancies
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == vacancyId &&
                    candidate.EmployerProfileId ==
                        employerProfile.Id,
                cancellationToken);

        if (vacancy is null)
        {
            return VacancyStatusUpdateResult.Failure(
                VacancyStatusUpdateFailureReason.NotFound);
        }

        if (vacancy.Status == VacancyStatus.Closed)
        {
            return VacancyStatusUpdateResult.Failure(
                VacancyStatusUpdateFailureReason.AlreadyClosed);
        }

        _dbContext.Entry(vacancy)
            .Property(candidate => candidate.RowVersion)
            .OriginalValue = request.RowVersion;

        try
        {
            vacancy.Close(_clock.UtcNow);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return VacancyStatusUpdateResult.Failure(
                VacancyStatusUpdateFailureReason
                    .ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return VacancyStatusUpdateResult.Failure(
                VacancyStatusUpdateFailureReason
                    .PersistenceFailed);
        }

        return VacancyStatusUpdateResult.Success(
            new VacancyStatusDto(
                vacancy.Id,
                vacancy.Status,
                vacancy.ClosedAtUtc,
                vacancy.RowVersion));
    }

    private async Task<bool> IsValidEmployerAsync(
        ApplicationUser? user,
        CancellationToken cancellationToken)
    {
        if (user is null)
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

    private static bool IsValidCreateRequest(
        CreateVacancyRequest request)
    {
        if (request is null ||
            !VacancyRules.IsValidTitle(request.Title) ||
            !VacancyRules.IsValidDescription(
                request.Description) ||
            !VacancyRules.IsValidLocation(
                request.Location) ||
            !VacancyRules.IsValidMinimumExperienceMonths(
                request.MinimumExperienceMonths) ||
            request.RequiredSkillIds is null ||
            !VacancyRules.IsValidRequiredSkillCount(
                request.RequiredSkillIds.Count))
        {
            return false;
        }

        if (request.RequiredEducationLevel.HasValue &&
            !Enum.IsDefined(
                typeof(EducationLevel),
                request.RequiredEducationLevel.Value))
        {
            return false;
        }

        if (request.RequiredSkillIds.Any(
                skillId => skillId == Guid.Empty))
        {
            return false;
        }

        return request.RequiredSkillIds
                   .Distinct()
                   .Count() ==
               request.RequiredSkillIds.Count;
    }

    private static VacancyDto ToVacancyDto(
        Vacancy vacancy,
        IReadOnlyCollection<Skill> skills)
    {
        var requiredSkills = skills
            .OrderBy(skill => skill.NormalizedName)
            .ThenBy(skill => skill.Id)
            .Select(skill =>
                new SkillSummaryDto(
                    skill.Id,
                    skill.Name))
            .ToArray();

        return new VacancyDto(
            vacancy.Id,
            vacancy.Title,
            vacancy.Description,
            vacancy.Location,
            vacancy.MinimumExperienceMonths,
            vacancy.RequiredEducationLevel,
            vacancy.Status,
            vacancy.PublishedAtUtc,
            vacancy.UpdatedAtUtc,
            vacancy.ClosedAtUtc,
            requiredSkills,
            vacancy.RowVersion);
    }
}