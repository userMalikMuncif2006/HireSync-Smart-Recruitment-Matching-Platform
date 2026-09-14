using HireSync.Domain.Enums;

namespace HireSync.Domain.Rules;

public static class VacancyRules
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 150;

    public const int DescriptionMinLength = 20;
    public const int DescriptionMaxLength = 5000;

    public const int LocationMinLength = 2;
    public const int LocationMaxLength = 100;

    public const int MinimumExperienceMonthsMin = 0;
    public const int MinimumExperienceMonthsMax = 720;

    public const int RequiredSkillsMinCount = 1;
    public const int RequiredSkillsMaxCount = 50;

    public static bool HasValidRequiredText(
        string? value,
        int minLength,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        return trimmed.Length >= minLength &&
               trimmed.Length <= maxLength;
    }

    public static bool IsValidTitle(string? title) =>
        HasValidRequiredText(
            title,
            TitleMinLength,
            TitleMaxLength);

    public static bool IsValidDescription(string? description) =>
        HasValidRequiredText(
            description,
            DescriptionMinLength,
            DescriptionMaxLength);

    public static bool IsValidLocation(string? location) =>
        HasValidRequiredText(
            location,
            LocationMinLength,
            LocationMaxLength);

    public static bool IsValidMinimumExperienceMonths(int months) =>
        months >= MinimumExperienceMonthsMin &&
        months <= MinimumExperienceMonthsMax;

    public static bool IsValidRequiredSkillCount(int count) =>
        count >= RequiredSkillsMinCount &&
        count <= RequiredSkillsMaxCount;

    public static bool CanUpdate(VacancyStatus status) =>
        status == VacancyStatus.Open;

    public static bool CanClose(VacancyStatus status) =>
        status == VacancyStatus.Open;

    public static bool CanTransition(
        VacancyStatus currentStatus,
        VacancyStatus requestedStatus)
    {
        return currentStatus == VacancyStatus.Open &&
               requestedStatus == VacancyStatus.Closed;
    }

    public static bool IsTerminal(VacancyStatus status) =>
        status == VacancyStatus.Closed;
}