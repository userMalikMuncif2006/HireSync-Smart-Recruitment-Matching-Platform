using HireSync.Application.DTOs.Matching;
using HireSync.Application.Interfaces.Time;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Matching;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using HireSync.Infrastructure.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class VacancyDetailServiceTests
{
    [Fact]
    public async Task Missing_cv_keeps_valid_score_but_disables_apply()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedReadyDetailAsync(
                environment,
                includeCv: false);

        var service =
            CreateService(
                environment);

        var withoutCv =
            await service.GetDetailAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.True(withoutCv.Succeeded);

        var firstDetail =
            Assert.IsType<
                HireSync.Application.DTOs.Vacancy.PublicVacancyDetailDto>(
                withoutCv.Detail);

        Assert.Equal(
            MatchStatusCodes.Ready,
            firstDetail.MatchStatus);

        Assert.NotNull(firstDetail.Match);

        Assert.Equal(
            100.00m,
            firstDetail.Match!.TotalScore);

        Assert.False(
            firstDetail.CanApply);

        Assert.False(
            firstDetail.HasApplied);

        environment.Context.CvDocuments.Add(
            CreateCvDocument(
                seeded.JobSeekerProfileId));

        await environment.Context
            .SaveChangesAsync();

        var withCv =
            await service.GetDetailAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.True(withCv.Succeeded);

        var secondDetail =
            Assert.IsType<
                HireSync.Application.DTOs.Vacancy.PublicVacancyDetailDto>(
                withCv.Detail);

        Assert.True(
            secondDetail.CanApply);

        Assert.False(
            secondDetail.HasApplied);

        Assert.NotNull(
            secondDetail.Match);

        Assert.Equal(
            firstDetail.Match.TotalScore,
            secondDetail.Match!.TotalScore);

        Assert.Equal(
            firstDetail.Match.SkillsScore,
            secondDetail.Match.SkillsScore);

        Assert.Equal(
            firstDetail.Match.ExperienceScore,
            secondDetail.Match.ExperienceScore);

        Assert.Equal(
            firstDetail.Match.EducationScore,
            secondDetail.Match.EducationScore);

        Assert.Equal(
            firstDetail.Match.LocationScore,
            secondDetail.Match.LocationScore);

        Assert.Equal(
            FixedComputedAtUtc,
            secondDetail.ComputedAtUtc);
    }

    [Fact]
    public async Task Existing_application_sets_applied_state_and_disables_apply()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedReadyDetailAsync(
                environment,
                includeCv: true);

        var service =
            CreateService(
                environment);

        var beforeApplication =
            await service.GetDetailAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        var beforeDetail =
            Assert.IsType<
                HireSync.Application.DTOs.Vacancy.PublicVacancyDetailDto>(
                beforeApplication.Detail);

        Assert.True(
            beforeDetail.CanApply);

        Assert.False(
            beforeDetail.HasApplied);

        environment.Context.JobApplications.Add(
            new JobApplication(
                Guid.NewGuid(),
                seeded.VacancyId,
                seeded.JobSeekerProfileId,
                Utc(11)));

        await environment.Context
            .SaveChangesAsync();

        var afterApplication =
            await service.GetDetailAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.True(
            afterApplication.Succeeded);

        var afterDetail =
            Assert.IsType<
                HireSync.Application.DTOs.Vacancy.PublicVacancyDetailDto>(
                afterApplication.Detail);

        Assert.False(
            afterDetail.CanApply);

        Assert.True(
            afterDetail.HasApplied);

        Assert.NotNull(
            afterDetail.Match);

        Assert.Equal(
            beforeDetail.Match!.TotalScore,
            afterDetail.Match!.TotalScore);
    }

    [Fact]
    public async Task Incomplete_profile_returns_success_with_null_match()
    {
        using var environment =
            CreateEnvironment();

        var vacancy =
            await SeedEligibleVacancyAsync(
                environment);

        var jobSeeker =
            CreateUser(
                AccountStatus.Active,
                verificationStatus: null,
                "incomplete@example.com");

        environment.Context.Users.Add(
            jobSeeker);

        var profile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                jobSeeker.Id,
                Utc(7));

        environment.Context.JobSeekerProfiles.Add(
            profile);

        await environment.Context
            .SaveChangesAsync();

        var service =
            CreateService(
                environment);

        var result =
            await service.GetDetailAsync(
                jobSeeker.Id,
                vacancy.VacancyId);

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            VacancyDetailFailureReason.None,
            result.FailureReason);

        var detail =
            Assert.IsType<
                HireSync.Application.DTOs.Vacancy.PublicVacancyDetailDto>(
                result.Detail);

        Assert.Equal(
            MatchStatusCodes.ProfileIncomplete,
            detail.MatchStatus);

        Assert.Null(
            detail.Match);

        Assert.False(
            detail.CanApply);

        Assert.False(
            detail.HasApplied);

        Assert.Contains(
            nameof(
                JobSeekerProfile
                    .TotalExperienceMonths),
            detail.MissingProfileFields);

        Assert.Contains(
            nameof(
                JobSeekerProfile
                    .EducationLevel),
            detail.MissingProfileFields);

        Assert.Contains(
            nameof(
                JobSeekerProfile
                    .PreferredLocation),
            detail.MissingProfileFields);

        Assert.Contains(
            "Skills",
            detail.MissingProfileFields);

        Assert.Equal(
            FixedComputedAtUtc,
            detail.ComputedAtUtc);
    }

    [Fact]
    public async Task Closed_vacancy_returns_unavailable()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedReadyDetailAsync(
                environment,
                includeCv: true);

        var vacancy =
            await environment.Context
                .Vacancies
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                        seeded.VacancyId);

        vacancy.Close(
            Utc(11));

        await environment.Context
            .SaveChangesAsync();

        var service =
            CreateService(
                environment);

        var result =
            await service.GetDetailAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.False(
            result.Succeeded);

        Assert.Null(
            result.Detail);

        Assert.Equal(
            VacancyDetailFailureReason
                .VacancyUnavailable,
            result.FailureReason);
    }

    private static VacancyDetailService
        CreateService(
            TestEnvironment environment)
    {
        return new VacancyDetailService(
            environment.Context,
            new MatchEngine(),
            new FakeClock(
                FixedComputedAtUtc));
    }

    private static async Task<SeededDetail>
        SeedReadyDetailAsync(
            TestEnvironment environment,
            bool includeCv)
    {
        var eligibleVacancy =
            await SeedEligibleVacancyAsync(
                environment);

        var jobSeeker =
            CreateUser(
                AccountStatus.Active,
                verificationStatus: null,
                $"jobseeker-{Guid.NewGuid()}@example.com");

        environment.Context.Users.Add(
            jobSeeker);

        var profile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                jobSeeker.Id,
                Utc(7));

        profile.UpdateStructuredProfile(
            totalExperienceMonths: 12,
            educationLevel:
                EducationLevel.Bachelor,
            preferredLocation:
                "Colombo",
            updatedAtUtc:
                Utc(8));

        environment.Context.JobSeekerProfiles.Add(
            profile);

        environment.Context.JobSeekerSkills.Add(
            new JobSeekerSkill(
                profile.Id,
                eligibleVacancy.SkillId));

        if (includeCv)
        {
            environment.Context.CvDocuments.Add(
                CreateCvDocument(
                    profile.Id));
        }

        await environment.Context
            .SaveChangesAsync();

        return new SeededDetail(
            jobSeeker.Id,
            profile.Id,
            eligibleVacancy.VacancyId);
    }

    private static async Task<SeededVacancy>
        SeedEligibleVacancyAsync(
            TestEnvironment environment)
    {
        var skill =
            new Skill(
                Guid.NewGuid(),
                "C#");

        environment.Context.Skills.Add(
            skill);

        var employer =
            CreateUser(
                AccountStatus.Active,
                EmployerVerificationStatus.Approved,
                $"employer-{Guid.NewGuid()}@example.com");

        environment.Context.Users.Add(
            employer);

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
                minimumExperienceMonths: 12,
                requiredEducationLevel:
                    EducationLevel.Bachelor,
                publishedAtUtc:
                    Utc(9));

        environment.Context.Vacancies.Add(
            vacancy);

        environment.Context.VacancySkills.Add(
            new VacancySkill(
                vacancy.Id,
                skill.Id));

        await environment.Context
            .SaveChangesAsync();

        return new SeededVacancy(
            vacancy.Id,
            skill.Id);
    }

    private static CvDocument
        CreateCvDocument(
            Guid profileId)
    {
        var storedFileName =
            $"{Guid.NewGuid():N}.pdf";

        return new CvDocument(
            Guid.NewGuid(),
            profileId,
            "candidate-cv.pdf",
            storedFileName,
            $"cvs/{storedFileName}",
            CvDocument.PdfExtension,
            CvDocument.PdfContentType,
            sizeBytes: 1_024,
            sha256Hash:
                new string('a', 64),
            uploadedAtUtc:
                Utc(10));
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
            DisplayName =
                "Test User",
            AccountStatus =
                accountStatus,
            EmployerVerificationStatus =
                verificationStatus,
            TokenVersion = 1,
            CreatedAtUtc =
                Utc(6),
            UpdatedAtUtc =
                Utc(6)
        };
    }

    private static EmployerProfile
        CreateEmployerProfile(
            Guid userId)
    {
        var registrationNumber =
            $"PV-{Guid.NewGuid():N}";

        return new EmployerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyName =
                "Eligible Company",
            NormalizedCompanyName =
                "ELIGIBLE COMPANY",
            Description =
                "A complete Employer company profile.",
            Location =
                "Colombo",
            NormalizedLocation =
                "COLOMBO",
            ContactPersonName =
                "Test Contact",
            ContactPersonDesignation =
                "HR Manager",
            BusinessRegistrationNumber =
                registrationNumber,
            NormalizedBusinessRegistrationNumber =
                registrationNumber.ToUpperInvariant(),
            MobileNumber =
                "0771234567",
            CompanyWebsite =
                "https://example.com",
            CreatedAtUtc =
                Utc(6),
            UpdatedAtUtc =
                Utc(6)
        };
    }

    private static TestEnvironment
        CreateEnvironment()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<
            HireSyncDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"HireSyncVacancyDetailTests-{Guid.NewGuid()}"));

        var provider =
            services.BuildServiceProvider();

        var scope =
            provider.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<
                    HireSyncDbContext>();

        context.Database
            .EnsureCreated();

        return new TestEnvironment(
            provider,
            scope,
            context);
    }

    private static readonly DateTime
        FixedComputedAtUtc =
            new(
                2026,
                9,
                12,
                12,
                30,
                0,
                DateTimeKind.Utc);

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

    private sealed record SeededDetail(
        Guid JobSeekerUserId,
        Guid JobSeekerProfileId,
        Guid VacancyId);

    private sealed record SeededVacancy(
        Guid VacancyId,
        Guid SkillId);

    private sealed class FakeClock
        : IClock
    {
        public FakeClock(
            DateTime utcNow)
        {
            UtcNow =
                utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class TestEnvironment
        : IDisposable
    {
        private readonly ServiceProvider
            _provider;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context)
        {
            _provider =
                provider;

            Scope =
                scope;

            Context =
                context;
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