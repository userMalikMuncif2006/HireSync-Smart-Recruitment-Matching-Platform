using HireSync.Application.DTOs.Matching;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Matching;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using HireSync.Infrastructure.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class JobMatchServiceTests
{
    [Fact]
    public async Task GetMatch_returns_expected_explainable_scores()
    {
        using var environment = CreateEnvironment();

        var candidateSkill =
            new Skill(Guid.NewGuid(), "C#");

        var missingSkill =
            new Skill(Guid.NewGuid(), "SQL");

        environment.Context.Skills.AddRange(
            candidateSkill,
            missingSkill);

        var jobSeeker =
            CreateUser(
                AccountStatus.Active,
                verificationStatus: null,
                "jobseeker@example.com");

        environment.Context.Users.Add(jobSeeker);

        var profile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                jobSeeker.Id,
                Utc(7));

        profile.UpdateStructuredProfile(
            totalExperienceMonths: 12,
            educationLevel: EducationLevel.Bachelor,
            preferredLocation: "Colombo",
            updatedAtUtc: Utc(8));

        environment.Context.JobSeekerProfiles.Add(profile);

        environment.Context.JobSeekerSkills.Add(
            new JobSeekerSkill(
                profile.Id,
                candidateSkill.Id));

        var employer =
            CreateUser(
                AccountStatus.Active,
                EmployerVerificationStatus.Approved,
                "employer@example.com");

        environment.Context.Users.Add(employer);

        var employerProfile =
            CreateEmployerProfile(
                employer.Id);

        environment.Context.EmployerProfiles.Add(
            employerProfile);

        var vacancy =
            new Vacancy(
                Guid.NewGuid(),
                employerProfile.Id,
                "Backend Developer",
                "Build and maintain secure backend software services.",
                "Colombo",
                minimumExperienceMonths: 24,
                requiredEducationLevel: EducationLevel.Bachelor,
                publishedAtUtc: Utc(9));

        environment.Context.Vacancies.Add(vacancy);

        environment.Context.VacancySkills.AddRange(
            new VacancySkill(
                vacancy.Id,
                candidateSkill.Id),
            new VacancySkill(
                vacancy.Id,
                missingSkill.Id));

        await environment.Context.SaveChangesAsync();

        var service =
            new JobMatchService(
                environment.Context,
                new MatchEngine());

        var result =
            await service.GetMatchAsync(
                jobSeeker.Id,
                vacancy.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(
            JobMatchFailureReason.None,
            result.FailureReason);

        var match =
            Assert.IsType<MatchResultDto>(
                result.Match);

        Assert.Equal(62.50m, match.TotalScore);
        Assert.Equal(25.00m, match.SkillsScore);
        Assert.Equal(12.50m, match.ExperienceScore);
        Assert.Equal(15.00m, match.EducationScore);
        Assert.Equal(10.00m, match.LocationScore);

        var matched =
            Assert.Single(match.MatchedSkills);

        Assert.Equal(
            candidateSkill.Id,
            matched.Id);

        var missing =
            Assert.Single(match.MissingSkills);

        Assert.Equal(
            missingSkill.Id,
            missing.Id);
    }

    [Fact]
    public async Task GetMatch_is_deterministic_for_identical_persisted_input()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedReadyMatchAsync(environment);

        var service =
            new JobMatchService(
                environment.Context,
                new MatchEngine());

        var first =
            await service.GetMatchAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        var second =
            await service.GetMatchAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);

        Assert.Equal(
            first.Match!.TotalScore,
            second.Match!.TotalScore);

        Assert.Equal(
            first.Match.SkillsScore,
            second.Match.SkillsScore);

        Assert.Equal(
            first.Match.ExperienceScore,
            second.Match.ExperienceScore);

        Assert.Equal(
            first.Match.EducationScore,
            second.Match.EducationScore);

        Assert.Equal(
            first.Match.LocationScore,
            second.Match.LocationScore);

        Assert.Equal(
            first.Match.MatchedSkills.Select(x => x.Id),
            second.Match.MatchedSkills.Select(x => x.Id));

        Assert.Equal(
            first.Match.MissingSkills.Select(x => x.Id),
            second.Match.MissingSkills.Select(x => x.Id));
    }

    [Fact]
    public async Task GetMatch_returns_job_seeker_not_ready_for_incomplete_profile()
    {
        using var environment = CreateEnvironment();

        var jobSeeker =
            CreateUser(
                AccountStatus.Active,
                verificationStatus: null,
                "incomplete@example.com");

        environment.Context.Users.Add(jobSeeker);

        environment.Context.JobSeekerProfiles.Add(
            new JobSeekerProfile(
                Guid.NewGuid(),
                jobSeeker.Id,
                Utc(7)));

        await environment.Context.SaveChangesAsync();

        var service =
            new JobMatchService(
                environment.Context,
                new MatchEngine());

        var result =
            await service.GetMatchAsync(
                jobSeeker.Id,
                Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Null(result.Match);
        Assert.Equal(
            JobMatchFailureReason.JobSeekerNotReady,
            result.FailureReason);
    }

    [Fact]
    public async Task GetMatch_returns_vacancy_unavailable_when_vacancy_is_closed()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedReadyMatchAsync(environment);

        var vacancy =
            await environment.Context.Vacancies
                .SingleAsync(candidate =>
                    candidate.Id == seeded.VacancyId);

        vacancy.Close(Utc(12));

        await environment.Context.SaveChangesAsync();

        var service =
            new JobMatchService(
                environment.Context,
                new MatchEngine());

        var result =
            await service.GetMatchAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.False(result.Succeeded);
        Assert.Null(result.Match);
        Assert.Equal(
            JobMatchFailureReason.VacancyUnavailable,
            result.FailureReason);
    }

    [Fact]
    public async Task GetMatch_rejects_empty_identifiers()
    {
        using var environment = CreateEnvironment();

        var service =
            new JobMatchService(
                environment.Context,
                new MatchEngine());

        var result =
            await service.GetMatchAsync(
                Guid.Empty,
                Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Null(result.Match);
        Assert.Equal(
            JobMatchFailureReason.InvalidInput,
            result.FailureReason);
    }

    private static async Task<SeededMatch>
        SeedReadyMatchAsync(
            TestEnvironment environment)
    {
        var csharp =
            new Skill(Guid.NewGuid(), "C#");

        var sql =
            new Skill(Guid.NewGuid(), "SQL");

        environment.Context.Skills.AddRange(
            csharp,
            sql);

        var jobSeeker =
            CreateUser(
                AccountStatus.Active,
                verificationStatus: null,
                $"jobseeker-{Guid.NewGuid()}@example.com");

        environment.Context.Users.Add(jobSeeker);

        var profile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                jobSeeker.Id,
                Utc(7));

        profile.UpdateStructuredProfile(
            24,
            EducationLevel.Bachelor,
            "Colombo",
            Utc(8));

        environment.Context.JobSeekerProfiles.Add(
            profile);

        environment.Context.JobSeekerSkills.AddRange(
            new JobSeekerSkill(
                profile.Id,
                csharp.Id),
            new JobSeekerSkill(
                profile.Id,
                sql.Id));

        var employer =
            CreateUser(
                AccountStatus.Active,
                EmployerVerificationStatus.Approved,
                $"employer-{Guid.NewGuid()}@example.com");

        environment.Context.Users.Add(employer);

        var employerProfile =
            CreateEmployerProfile(
                employer.Id);

        environment.Context.EmployerProfiles.Add(
            employerProfile);

        var vacancy =
            new Vacancy(
                Guid.NewGuid(),
                employerProfile.Id,
                "Software Engineer",
                "Build reliable enterprise software systems and services.",
                "Colombo",
                24,
                EducationLevel.Bachelor,
                Utc(9));

        environment.Context.Vacancies.Add(
            vacancy);

        environment.Context.VacancySkills.AddRange(
            new VacancySkill(
                vacancy.Id,
                csharp.Id),
            new VacancySkill(
                vacancy.Id,
                sql.Id));

        await environment.Context.SaveChangesAsync();

        return new SeededMatch(
            jobSeeker.Id,
            vacancy.Id);
    }

    private static ApplicationUser CreateUser(
        AccountStatus accountStatus,
        EmployerVerificationStatus? verificationStatus,
        string email)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName =
                email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail =
                email.ToUpperInvariant(),
            DisplayName = "Test User",
            AccountStatus = accountStatus,
            EmployerVerificationStatus =
                verificationStatus,
            TokenVersion = 1,
            CreatedAtUtc = Utc(6),
            UpdatedAtUtc = Utc(6)
        };
    }

    private static EmployerProfile CreateEmployerProfile(
        Guid userId)
    {
        return new EmployerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyName = "Eligible Company",
            NormalizedCompanyName =
                "ELIGIBLE COMPANY",
            Description =
                "A complete Employer company profile.",
            Location = "Colombo",
            NormalizedLocation = "COLOMBO",
            ContactPersonName = "Test Contact",
            ContactPersonDesignation = "HR Manager",
            BusinessRegistrationNumber =
                $"PV-{Guid.NewGuid():N}",
            NormalizedBusinessRegistrationNumber =
                $"PV-{Guid.NewGuid():N}".ToUpperInvariant(),
            MobileNumber = "0771234567",
            CompanyWebsite = "https://example.com",
            CreatedAtUtc = Utc(6),
            UpdatedAtUtc = Utc(6)
        };
    }

    private static TestEnvironment CreateEnvironment()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<HireSyncDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"HireSyncJobMatchTests-{Guid.NewGuid()}"));

        var provider =
            services.BuildServiceProvider();

        var scope =
            provider.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<HireSyncDbContext>();

        context.Database.EnsureCreated();

        return new TestEnvironment(
            provider,
            scope,
            context);
    }

    private static DateTime Utc(
        int hour) =>
        new(
            2026,
            9,
            12,
            hour,
            0,
            0,
            DateTimeKind.Utc);

    private sealed record SeededMatch(
        Guid JobSeekerUserId,
        Guid VacancyId);

    private sealed class TestEnvironment
        : IDisposable
    {
        private readonly ServiceProvider _provider;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context)
        {
            _provider = provider;
            Scope = scope;
            Context = context;
        }

        public IServiceScope Scope { get; }

        public HireSyncDbContext Context { get; }

        public void Dispose()
        {
            Scope.Dispose();
            _provider.Dispose();
        }
    }
}
