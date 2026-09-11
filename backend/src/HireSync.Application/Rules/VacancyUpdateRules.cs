using HireSync.Application.DTOs.Vacancy;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;

namespace HireSync.Application.Rules;

public static class VacancyUpdateRules
{
    public static bool HasValidRowVersion(
        byte[]? rowVersion) =>
        rowVersion is { Length: > 0 };

    public static bool IsValid(
        UpdateVacancyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!VacancyRules.IsValidTitle(request.Title) ||
            !VacancyRules.IsValidDescription(request.Description) ||
            !VacancyRules.IsValidLocation(request.Location) ||
            !VacancyRules.IsValidMinimumExperienceMonths(
                request.MinimumExperienceMonths) ||
            request.RequiredSkillIds is null ||
            !VacancyRules.IsValidRequiredSkillCount(
                request.RequiredSkillIds.Count) ||
            !HasValidRowVersion(request.RowVersion))
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
}