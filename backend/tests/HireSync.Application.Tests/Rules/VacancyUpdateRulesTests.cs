using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Rules;
using HireSync.Domain.Enums;

namespace HireSync.Application.Tests.Rules;

public sealed class VacancyUpdateRulesTests
{
    [Fact]
    public void IsValid_returns_true_for_valid_request()
    {
        var request =
            CreateValidRequest();

        Assert.True(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_allows_no_education_requirement()
    {
        var request =
            CreateValidRequest(
                requiredEducationLevel: null);

        Assert.True(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_when_row_version_missing()
    {
        var request =
            CreateValidRequest(
                rowVersion: Array.Empty<byte>());

        Assert.False(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_duplicate_skill_ids()
    {
        var skillId =
            Guid.NewGuid();

        var request =
            CreateValidRequest(
                requiredSkillIds:
                [
                    skillId,
                    skillId
                ]);

        Assert.False(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_empty_skill_id()
    {
        var request =
            CreateValidRequest(
                requiredSkillIds:
                [
                    Guid.Empty
                ]);

        Assert.False(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_no_required_skills()
    {
        var request =
            CreateValidRequest(
                requiredSkillIds:
                    Array.Empty<Guid>());

        Assert.False(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_too_many_required_skills()
    {
        var skillIds =
            Enumerable.Range(0, 51)
                .Select(_ => Guid.NewGuid())
                .ToArray();

        var request =
            CreateValidRequest(
                requiredSkillIds: skillIds);

        Assert.False(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_invalid_education_level()
    {
        var request =
            CreateValidRequest(
                requiredEducationLevel:
                    (EducationLevel)99);

        Assert.False(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_invalid_experience()
    {
        var request =
            CreateValidRequest(
                minimumExperienceMonths: 721);

        Assert.False(
            VacancyUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_invalid_title()
    {
        var request =
            new UpdateVacancyRequest(
                "A",
                "Build and maintain secure backend application services.",
                "Colombo",
                24,
                EducationLevel.Bachelor,
                new[]
                {
                    Guid.NewGuid()
                },
                new byte[]
                {
                    1, 2, 3, 4,
                    5, 6, 7, 8
                });

        Assert.False(
            VacancyUpdateRules.IsValid(request));
    }

    private static UpdateVacancyRequest CreateValidRequest(
        int minimumExperienceMonths = 24,
        EducationLevel? requiredEducationLevel =
            EducationLevel.Bachelor,
        IReadOnlyList<Guid>? requiredSkillIds = null,
        byte[]? rowVersion = null)
    {
        return new UpdateVacancyRequest(
            "Backend Developer",
            "Build and maintain secure backend application services.",
            "Colombo",
            minimumExperienceMonths,
            requiredEducationLevel,
            requiredSkillIds ??
            [
                Guid.NewGuid()
            ],
            rowVersion ??
            [
                1, 2, 3, 4,
                5, 6, 7, 8
            ]);
    }
}