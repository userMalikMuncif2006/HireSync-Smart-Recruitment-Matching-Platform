using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests.Entities;

public sealed class JobSeekerProfileTests
{
    private static readonly DateTime CreatedAt =
        new(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime UpdatedAt =
        new(2026, 9, 8, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewProfile_StartsWithUnsetMatchingInputs()
    {
        var profile = new JobSeekerProfile(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAt);

        Assert.Null(profile.TotalExperienceMonths);
        Assert.Null(profile.EducationLevel);
        Assert.Null(profile.PreferredLocation);
        Assert.Null(profile.NormalizedPreferredLocation);
        Assert.False(profile.IsMatchReady(false));
        Assert.False(profile.IsMatchReady(true));
    }

    [Fact]
    public void UpdateStructuredProfile_AcceptsZeroExperienceAndNoFormalQualification()
    {
        var profile = CreateProfile();

        profile.UpdateStructuredProfile(
            0,
            EducationLevel.NoFormalQualification,
            "  Colombo   Central  ",
            UpdatedAt);

        Assert.Equal(0, profile.TotalExperienceMonths);
        Assert.Equal(
            EducationLevel.NoFormalQualification,
            profile.EducationLevel);
        Assert.Equal("Colombo Central", profile.PreferredLocation);
        Assert.Equal(
            "COLOMBO CENTRAL",
            profile.NormalizedPreferredLocation);
        Assert.Equal(UpdatedAt, profile.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateStructuredProfile_AcceptsMaximumExperience()
    {
        var profile = CreateProfile();

        profile.UpdateStructuredProfile(
            720,
            EducationLevel.Bachelor,
            "Colombo",
            UpdatedAt);

        Assert.Equal(720, profile.TotalExperienceMonths);
    }

    [Fact]
    public void UpdateStructuredProfile_NegativeExperience_Throws()
    {
        var profile = CreateProfile();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => profile.UpdateStructuredProfile(
                -1,
                EducationLevel.Bachelor,
                "Colombo",
                UpdatedAt));
    }

    [Fact]
    public void UpdateStructuredProfile_ExperienceAboveMaximum_Throws()
    {
        var profile = CreateProfile();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => profile.UpdateStructuredProfile(
                721,
                EducationLevel.Bachelor,
                "Colombo",
                UpdatedAt));
    }

    [Fact]
    public void UpdateStructuredProfile_InvalidEducationLevel_Throws()
    {
        var profile = CreateProfile();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => profile.UpdateStructuredProfile(
                12,
                (EducationLevel)99,
                "Colombo",
                UpdatedAt));
    }

    [Fact]
    public void UpdateStructuredProfile_BlankLocation_Throws()
    {
        var profile = CreateProfile();

        Assert.Throws<ArgumentException>(
            () => profile.UpdateStructuredProfile(
                12,
                EducationLevel.Diploma,
                "   ",
                UpdatedAt));
    }

    [Fact]
    public void IsMatchReady_RequiresAllStructuredInputsAndSkill()
    {
        var profile = CreateProfile();

        profile.UpdateStructuredProfile(
            12,
            EducationLevel.Diploma,
            "Colombo",
            UpdatedAt);

        Assert.False(profile.IsMatchReady(false));
        Assert.True(profile.IsMatchReady(true));
    }

    [Fact]
    public void Constructor_NonUtcCreatedTimestamp_Throws()
    {
        var localTime = new DateTime(
            2026,
            9,
            8,
            8,
            0,
            0,
            DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => new JobSeekerProfile(
                Guid.NewGuid(),
                Guid.NewGuid(),
                localTime));
    }

    [Fact]
    public void UpdateStructuredProfile_NonUtcUpdatedTimestamp_Throws()
    {
        var profile = CreateProfile();

        var localTime = new DateTime(
            2026,
            9,
            8,
            9,
            0,
            0,
            DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => profile.UpdateStructuredProfile(
                12,
                EducationLevel.Bachelor,
                "Colombo",
                localTime));
    }

    private static JobSeekerProfile CreateProfile()
    {
        return new JobSeekerProfile(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAt);
    }
}