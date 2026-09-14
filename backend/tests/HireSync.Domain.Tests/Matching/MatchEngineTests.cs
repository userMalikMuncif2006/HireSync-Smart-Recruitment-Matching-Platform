using HireSync.Domain.Enums;
using HireSync.Domain.Matching;

namespace HireSync.Domain.Tests.Matching;

public sealed class MatchEngineTests
{
    private readonly MatchEngine _engine = new();

    [Fact]
    public void Calculate_PerfectFit_Returns100()
    {
        var angular = Skill("Angular", 1);
        var csharp = Skill("C#", 2);
        var sql = Skill("SQL", 3);

        var candidate = Candidate(
            [csharp, angular, sql],
            36,
            EducationLevel.Bachelor,
            "COLOMBO");

        var vacancy = Vacancy(
            [csharp, angular],
            24,
            EducationLevel.Diploma,
            "COLOMBO");

        var result = _engine.Calculate(candidate, vacancy);

        Assert.Equal(100.00m, result.TotalScore);
        Assert.Equal(50.00m, result.SkillsScore);
        Assert.Equal(25.00m, result.ExperienceScore);
        Assert.Equal(15.00m, result.EducationScore);
        Assert.Equal(10.00m, result.LocationScore);

        Assert.Equal(
            ["Angular", "C#"],
            result.MatchedSkills.Select(x => x.Name));

        Assert.Empty(result.MissingSkills);
    }

    [Fact]
    public void Calculate_PartialFit_Returns6875()
    {
        var angular = Skill("Angular", 1);
        var csharp = Skill("C#", 2);
        var docker = Skill("Docker", 3);
        var git = Skill("Git", 4);
        var sql = Skill("SQL", 5);

        var candidate = Candidate(
            [csharp, angular, sql],
            18,
            EducationLevel.Bachelor,
            "COLOMBO");

        var vacancy = Vacancy(
            [csharp, angular, docker, git],
            24,
            EducationLevel.Diploma,
            "COLOMBO");

        var result = _engine.Calculate(candidate, vacancy);

        Assert.Equal(68.75m, result.TotalScore);
        Assert.Equal(25.00m, result.SkillsScore);
        Assert.Equal(18.75m, result.ExperienceScore);
        Assert.Equal(15.00m, result.EducationScore);
        Assert.Equal(10.00m, result.LocationScore);

        Assert.Equal(
            ["Docker", "Git"],
            result.MissingSkills.Select(x => x.Name));
    }

    [Fact]
    public void Calculate_LowFit_Returns1250()
    {
        var candidate = Candidate(
            [Skill("SQL", 1)],
            12,
            EducationLevel.Certificate,
            "KANDY");

        var vacancy = Vacancy(
            [Skill("C#", 2), Skill("Angular", 3)],
            24,
            EducationLevel.Bachelor,
            "COLOMBO");

        var result = _engine.Calculate(candidate, vacancy);

        Assert.Equal(12.50m, result.TotalScore);
        Assert.Equal(0m, result.SkillsScore);
        Assert.Equal(12.50m, result.ExperienceScore);
        Assert.Equal(0m, result.EducationScore);
        Assert.Equal(0m, result.LocationScore);
    }

    [Fact]
    public void Calculate_RoundingBoundary_RoundsFinalTotalTo6667()
    {
        var candidate = Candidate(
            [
                Skill("A", 1),
                Skill("B", 2)
            ],
            1,
            EducationLevel.Bachelor,
            "COLOMBO");

        var vacancy = Vacancy(
            [
                Skill("A", 1),
                Skill("B", 2),
                Skill("C", 3)
            ],
            3,
            EducationLevel.Diploma,
            "COLOMBO");

        var result = _engine.Calculate(candidate, vacancy);

        Assert.Equal(66.67m, result.TotalScore);
        Assert.Equal(33.33m, result.SkillsScore);
        Assert.Equal(8.33m, result.ExperienceScore);
    }

    [Fact]
    public void Calculate_MidpointBoundary_UsesAwayFromZero()
    {
        var candidateSkills = Enumerable
            .Range(1, 5)
            .Select(i => Skill($"S{i}", i))
            .ToArray();

        var requiredSkills = Enumerable
            .Range(1, 8)
            .Select(i => Skill($"S{i}", i))
            .ToArray();

        var candidate = Candidate(
            candidateSkills,
            7,
            EducationLevel.Bachelor,
            "COLOMBO");

        var vacancy = Vacancy(
            requiredSkills,
            8,
            EducationLevel.Diploma,
            "COLOMBO");

        var result = _engine.Calculate(candidate, vacancy);

        Assert.Equal(78.13m, result.TotalScore);
    }

    [Fact]
    public void Calculate_DuplicateSkillIds_CountsOnce()
    {
        var csharp = Skill("C#", 1);
        var angular = Skill("Angular", 2);

        var candidate = Candidate(
            [csharp, csharp, angular],
            12,
            EducationLevel.Bachelor,
            "COLOMBO");

        var vacancy = Vacancy(
            [csharp, csharp, angular],
            12,
            EducationLevel.Bachelor,
            "COLOMBO");

        var result = _engine.Calculate(candidate, vacancy);

        Assert.Equal(50m, result.SkillsScore);
        Assert.Equal(2, result.MatchedSkills.Count);
    }

    [Fact]
    public void Calculate_RequiredExperienceZero_GivesFull25()
    {
        var skill = Skill("C#", 1);

        var result = _engine.Calculate(
            Candidate(
                [skill],
                0,
                EducationLevel.NoFormalQualification,
                "COLOMBO"),
            Vacancy(
                [skill],
                0,
                null,
                "COLOMBO"));

        Assert.Equal(25m, result.ExperienceScore);
    }

    [Fact]
    public void Calculate_ExperienceAboveRequirement_IsCappedAt25()
    {
        var skill = Skill("C#", 1);

        var result = _engine.Calculate(
            Candidate(
                [skill],
                48,
                EducationLevel.Bachelor,
                "COLOMBO"),
            Vacancy(
                [skill],
                12,
                EducationLevel.Diploma,
                "COLOMBO"));

        Assert.Equal(25m, result.ExperienceScore);
    }

    [Fact]
    public void Calculate_NoRequiredEducation_GivesFull15()
    {
        var skill = Skill("C#", 1);

        var result = _engine.Calculate(
            Candidate(
                [skill],
                0,
                EducationLevel.NoFormalQualification,
                "COLOMBO"),
            Vacancy(
                [skill],
                0,
                null,
                "COLOMBO"));

        Assert.Equal(15m, result.EducationScore);
    }

    [Theory]
    [InlineData(EducationLevel.Diploma, 0)]
    [InlineData(EducationLevel.Bachelor, 15)]
    [InlineData(EducationLevel.Master, 15)]
    public void Calculate_EducationMinimum_IsBinary(
        EducationLevel candidateEducation,
        int expected)
    {
        var skill = Skill("C#", 1);

        var result = _engine.Calculate(
            Candidate(
                [skill],
                0,
                candidateEducation,
                "COLOMBO"),
            Vacancy(
                [skill],
                0,
                EducationLevel.Bachelor,
                "COLOMBO"));

        Assert.Equal((decimal)expected, result.EducationScore);
    }

    [Fact]
    public void Calculate_LocationExactNormalizedEquality_Is10Otherwise0()
    {
        var skill = Skill("C#", 1);

        var exact = _engine.Calculate(
            Candidate(
                [skill],
                0,
                EducationLevel.Bachelor,
                "COLOMBO"),
            Vacancy(
                [skill],
                0,
                null,
                "COLOMBO"));

        var different = _engine.Calculate(
            Candidate(
                [skill],
                0,
                EducationLevel.Bachelor,
                "KANDY"),
            Vacancy(
                [skill],
                0,
                null,
                "COLOMBO"));

        Assert.Equal(10m, exact.LocationScore);
        Assert.Equal(0m, different.LocationScore);
    }

    [Fact]
    public void VacancyInput_EmptyRequiredSkills_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => Vacancy(
                [],
                0,
                null,
                "COLOMBO"));
    }

    [Fact]
    public void Calculate_SameInputRepeated1000Times_IsStable()
    {
        var candidate = Candidate(
            [Skill("C#", 1), Skill("Angular", 2)],
            18,
            EducationLevel.Bachelor,
            "COLOMBO");

        var vacancy = Vacancy(
            [Skill("C#", 1), Skill("Docker", 3)],
            24,
            EducationLevel.Diploma,
            "COLOMBO");

        var expected = _engine.Calculate(candidate, vacancy);

        for (var i = 0; i < 1000; i++)
        {
            var actual = _engine.Calculate(candidate, vacancy);

            Assert.Equal(expected.TotalScore, actual.TotalScore);
            Assert.Equal(expected.SkillsScore, actual.SkillsScore);
            Assert.Equal(expected.ExperienceScore, actual.ExperienceScore);
            Assert.Equal(expected.EducationScore, actual.EducationScore);
            Assert.Equal(expected.LocationScore, actual.LocationScore);

            Assert.Equal(
                expected.MatchedSkills.Select(x => x.Id),
                actual.MatchedSkills.Select(x => x.Id));

            Assert.Equal(
                expected.MissingSkills.Select(x => x.Id),
                actual.MissingSkills.Select(x => x.Id));
        }
    }

    [Fact]
    public void Calculate_ConflictingCanonicalDataForSameSkillId_IsRejected()
    {
        var id = SkillId(1);

        var first = new MatchSkill(
            id,
            "C#",
            "C#");

        var conflicting = new MatchSkill(
            id,
            "Dotnet",
            "DOTNET");

        var candidate = Candidate(
            [first, conflicting],
            12,
            EducationLevel.Bachelor,
            "COLOMBO");

        var vacancy = Vacancy(
            [first],
            12,
            EducationLevel.Bachelor,
            "COLOMBO");

        Assert.Throws<InvalidOperationException>(
            () => _engine.Calculate(candidate, vacancy));
    }

    private static CandidateMatchInput Candidate(
        IEnumerable<MatchSkill> skills,
        int experienceMonths,
        EducationLevel educationLevel,
        string location)
    {
        return new CandidateMatchInput(
            skills,
            experienceMonths,
            educationLevel,
            location);
    }

    private static VacancyMatchInput Vacancy(
        IEnumerable<MatchSkill> skills,
        int experienceMonths,
        EducationLevel? educationLevel,
        string location)
    {
        return new VacancyMatchInput(
            skills,
            experienceMonths,
            educationLevel,
            location);
    }

    private static MatchSkill Skill(
        string name,
        int id)
    {
        return new MatchSkill(
            SkillId(id),
            name,
            name.ToUpperInvariant());
    }

    private static Guid SkillId(int value)
    {
        return Guid.Parse(
            $"00000000-0000-0000-0000-{value:000000000000}");
    }
}
