using HireSync.Domain.Entities;
using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests.Entities;

public sealed class VacancyTests
{
    private static readonly DateTime PublishedAt =
        new(2026, 9, 11, 8, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime UpdatedAt =
        new(2026, 9, 11, 9, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ClosedAt =
        new(2026, 9, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidValues_CreatesOpenVacancy()
    {
        var vacancyId = Guid.NewGuid();
        var employerProfileId = Guid.NewGuid();

        var vacancy = new Vacancy(
            vacancyId,
            employerProfileId,
            "Software Engineer",
            "Build and maintain modern software applications.",
            "  Colombo   Central  ",
            24,
            EducationLevel.Bachelor,
            PublishedAt);

        Assert.Equal(vacancyId, vacancy.Id);
        Assert.Equal(employerProfileId, vacancy.EmployerProfileId);
        Assert.Equal("Software Engineer", vacancy.Title);
        Assert.Equal(
            "Build and maintain modern software applications.",
            vacancy.Description);
        Assert.Equal("Colombo Central", vacancy.Location);
        Assert.Equal("COLOMBO CENTRAL", vacancy.NormalizedLocation);
        Assert.Equal(24, vacancy.MinimumExperienceMonths);
        Assert.Equal(
            EducationLevel.Bachelor,
            vacancy.RequiredEducationLevel);
        Assert.Equal(VacancyStatus.Open, vacancy.Status);
        Assert.Equal(PublishedAt, vacancy.PublishedAtUtc);
        Assert.Equal(PublishedAt, vacancy.UpdatedAtUtc);
        Assert.Null(vacancy.ClosedAtUtc);
        Assert.Empty(vacancy.RowVersion);
    }

    [Fact]
    public void Constructor_AllowsZeroExperienceAndNoEducationRequirement()
    {
        var vacancy = new Vacancy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Junior Developer",
            "Suitable for candidates beginning their software career.",
            "Colombo",
            0,
            null,
            PublishedAt);

        Assert.Equal(0, vacancy.MinimumExperienceMonths);
        Assert.Null(vacancy.RequiredEducationLevel);
    }

    [Fact]
    public void Constructor_AllowsMaximumExperience()
    {
        var vacancy = CreateVacancy(
            minimumExperienceMonths: 720);

        Assert.Equal(720, vacancy.MinimumExperienceMonths);
    }

    [Fact]
    public void Constructor_WithEmptyVacancyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vacancy(
                Guid.Empty,
                Guid.NewGuid(),
                "Software Engineer",
                "Build and maintain modern software applications.",
                "Colombo",
                12,
                EducationLevel.Bachelor,
                PublishedAt));
    }

    [Fact]
    public void Constructor_WithEmptyEmployerProfileId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vacancy(
                Guid.NewGuid(),
                Guid.Empty,
                "Software Engineer",
                "Build and maintain modern software applications.",
                "Colombo",
                12,
                EducationLevel.Bachelor,
                PublishedAt));
    }

    [Fact]
    public void Constructor_WithInvalidTitle_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vacancy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "A",
                "Build and maintain modern software applications.",
                "Colombo",
                12,
                EducationLevel.Bachelor,
                PublishedAt));
    }

    [Fact]
    public void Constructor_WithInvalidDescription_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vacancy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineer",
                "Too short",
                "Colombo",
                12,
                EducationLevel.Bachelor,
                PublishedAt));
    }

    [Fact]
    public void Constructor_WithInvalidLocation_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vacancy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineer",
                "Build and maintain modern software applications.",
                " ",
                12,
                EducationLevel.Bachelor,
                PublishedAt));
    }

    [Fact]
    public void Constructor_WithNegativeExperience_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Vacancy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineer",
                "Build and maintain modern software applications.",
                "Colombo",
                -1,
                EducationLevel.Bachelor,
                PublishedAt));
    }

    [Fact]
    public void Constructor_WithExperienceAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Vacancy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineer",
                "Build and maintain modern software applications.",
                "Colombo",
                721,
                EducationLevel.Bachelor,
                PublishedAt));
    }

    [Fact]
    public void Constructor_WithInvalidEducationLevel_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Vacancy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineer",
                "Build and maintain modern software applications.",
                "Colombo",
                12,
                (EducationLevel)99,
                PublishedAt));
    }

    [Fact]
    public void Constructor_WithNonUtcPublishedTimestamp_ThrowsArgumentException()
    {
        var localTime = new DateTime(
            2026,
            9,
            11,
            8,
            0,
            0,
            DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            new Vacancy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineer",
                "Build and maintain modern software applications.",
                "Colombo",
                12,
                EducationLevel.Bachelor,
                localTime));
    }

    [Fact]
    public void UpdateRequirements_WhenOpen_UpdatesStructuredRequirements()
    {
        var vacancy = CreateVacancy();

        vacancy.UpdateRequirements(
            "Senior Backend Engineer",
            "Design and maintain scalable backend software services.",
            "  Kandy   City  ",
            60,
            EducationLevel.Master,
            UpdatedAt);

        Assert.Equal("Senior Backend Engineer", vacancy.Title);
        Assert.Equal(
            "Design and maintain scalable backend software services.",
            vacancy.Description);
        Assert.Equal("Kandy City", vacancy.Location);
        Assert.Equal("KANDY CITY", vacancy.NormalizedLocation);
        Assert.Equal(60, vacancy.MinimumExperienceMonths);
        Assert.Equal(
            EducationLevel.Master,
            vacancy.RequiredEducationLevel);
        Assert.Equal(UpdatedAt, vacancy.UpdatedAtUtc);
        Assert.Equal(VacancyStatus.Open, vacancy.Status);
    }

    [Fact]
    public void UpdateRequirements_WithNonUtcTimestamp_ThrowsArgumentException()
    {
        var vacancy = CreateVacancy();

        var localTime = new DateTime(
            2026,
            9,
            11,
            9,
            0,
            0,
            DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            vacancy.UpdateRequirements(
                "Senior Backend Engineer",
                "Design and maintain scalable backend software services.",
                "Kandy",
                60,
                EducationLevel.Master,
                localTime));
    }

    [Fact]
    public void Close_WhenOpen_ClosesVacancy()
    {
        var vacancy = CreateVacancy();

        vacancy.Close(ClosedAt);

        Assert.Equal(VacancyStatus.Closed, vacancy.Status);
        Assert.Equal(ClosedAt, vacancy.ClosedAtUtc);
        Assert.Equal(ClosedAt, vacancy.UpdatedAtUtc);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ThrowsInvalidOperationException()
    {
        var vacancy = CreateVacancy();

        vacancy.Close(ClosedAt);

        Assert.Throws<InvalidOperationException>(() =>
            vacancy.Close(
                ClosedAt.AddHours(1)));
    }

    [Fact]
    public void Close_WithNonUtcTimestamp_ThrowsArgumentException()
    {
        var vacancy = CreateVacancy();

        var localTime = new DateTime(
            2026,
            9,
            11,
            10,
            0,
            0,
            DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            vacancy.Close(localTime));
    }

    [Fact]
    public void UpdateRequirements_WhenClosed_ThrowsInvalidOperationException()
    {
        var vacancy = CreateVacancy();

        vacancy.Close(ClosedAt);

        Assert.Throws<InvalidOperationException>(() =>
            vacancy.UpdateRequirements(
                "Senior Backend Engineer",
                "Design and maintain scalable backend software services.",
                "Kandy",
                60,
                EducationLevel.Master,
                ClosedAt.AddHours(1)));
    }

    private static Vacancy CreateVacancy(
        int minimumExperienceMonths = 24)
    {
        return new Vacancy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Software Engineer",
            "Build and maintain modern software applications.",
            "Colombo",
            minimumExperienceMonths,
            EducationLevel.Bachelor,
            PublishedAt);
    }
}