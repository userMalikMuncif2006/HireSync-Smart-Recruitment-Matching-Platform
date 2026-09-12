using HireSync.Application.DTOs.Applications;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Applications;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class JobApplicationServiceTests
{
    [Fact]
    public async Task Eligible_job_seeker_can_apply_once()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedScenarioAsync(
                environment);

        var service =
            CreateService(
                environment);

        var result =
            await service.CreateAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            ApplicationCreateFailureReason.None,
            result.FailureReason);

        var created =
            Assert.IsType<ApplicationCreatedDto>(
                result.Application);

        Assert.Equal(
            seeded.VacancyId,
            created.VacancyId);

        Assert.Equal(
            ApplicationStatus.Applied,
            created.Status);

        Assert.Equal(
            environment.Clock.UtcNow,
            created.AppliedAtUtc);

        Assert.Equal(
            environment.Clock.UtcNow,
            created.UpdatedAtUtc);

        environment.Context
            .ChangeTracker
            .Clear();

        var saved =
            await environment.Context
                .JobApplications
                .SingleAsync();

        Assert.Equal(
            created.Id,
            saved.Id);

        Assert.Equal(
            seeded.JobSeekerProfileId,
            saved.JobSeekerProfileId);

        Assert.Equal(
            ApplicationStatus.Applied,
            saved.Status);
    }

    [Fact]
    public async Task Second_application_for_same_vacancy_is_blocked()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedScenarioAsync(
                environment);

        var service =
            CreateService(
                environment);

        var first =
            await service.CreateAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        var second =
            await service.CreateAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.True(
            first.Succeeded);

        Assert.False(
            second.Succeeded);

        Assert.Equal(
            ApplicationCreateFailureReason.AlreadyApplied,
            second.FailureReason);

        Assert.Equal(
            1,
            await environment.Context
                .JobApplications
                .CountAsync());
    }

    [Fact]
    public async Task Current_cv_is_required_to_apply()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedScenarioAsync(
                environment,
                includeCv: false);

        var service =
            CreateService(
                environment);

        var result =
            await service.CreateAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            ApplicationCreateFailureReason.CurrentCvRequired,
            result.FailureReason);

        Assert.Empty(
            await environment.Context
                .JobApplications
                .ToListAsync());
    }

    [Fact]
    public async Task Incomplete_profile_cannot_apply()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedScenarioAsync(
                environment,
                profileReady: false);

        var service =
            CreateService(
                environment);

        var result =
            await service.CreateAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            ApplicationCreateFailureReason.ProfileNotReady,
            result.FailureReason);

        Assert.Empty(
            await environment.Context
                .JobApplications
                .ToListAsync());
    }

    [Fact]
    public async Task Closed_vacancy_cannot_receive_new_application()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedScenarioAsync(
                environment);

        var vacancy =
            await environment.Context
                .Vacancies
                .SingleAsync(
                    candidate =>
                        candidate.Id ==
                        seeded.VacancyId);

        vacancy.Close(
            Utc(13));

        await environment.Context
            .SaveChangesAsync();

        var service =
            CreateService(
                environment);

        var result =
            await service.CreateAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            ApplicationCreateFailureReason.VacancyClosed,
            result.FailureReason);

        Assert.Empty(
            await environment.Context
                .JobApplications
                .ToListAsync());
    }

    [Fact]
    public async Task Suspended_job_seeker_cannot_apply()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedScenarioAsync(
                environment,
                jobSeekerStatus:
                    AccountStatus.Suspended);

        var service =
            CreateService(
                environment);

        var result =
            await service.CreateAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            ApplicationCreateFailureReason.JobSeekerUnavailable,
            result.FailureReason);

        Assert.Empty(
            await environment.Context
                .JobApplications
                .ToListAsync());
    }

    [Fact]
    public async Task Unapproved_employer_makes_vacancy_unavailable()
    {
        using var environment =
            CreateEnvironment();

        var seeded =
            await SeedScenarioAsync(
                environment,
                employerVerificationStatus:
                    EmployerVerificationStatus.Pending);

        var service =
            CreateService(
                environment);

        var result =
            await service.CreateAsync(
                seeded.JobSeekerUserId,
                seeded.VacancyId);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            ApplicationCreateFailureReason.VacancyUnavailable,
            result.FailureReason);

        Assert.Empty(
            await environment.Context
                .JobApplications
                .ToListAsync());
    }

    private static JobApplicationService
        CreateService(
            TestEnvironment environment)
    {
        return new JobApplicationService(
            environment.Context,
            environment.UserManager,
            environment.Clock);
    }

    private static async Task<SeededScenario>
        SeedScenarioAsync(
            TestEnvironment environment,
            bool includeCv = true,
            bool profileReady = true,
            AccountStatus jobSeekerStatus =
                AccountStatus.Active,
            EmployerVerificationStatus
                employerVerificationStatus =
                    EmployerVerificationStatus.Approved)
    {
        await EnsureRoleAsync(
            environment,
            RoleNames.JobSeeker);

        await EnsureRoleAsync(
            environment,
            RoleNames.Employer);

        var jobSeeker =
            await CreateUserWithRoleAsync(
                environment,
                $"jobseeker-{Guid.NewGuid()}@example.com",
                "Test Job Seeker",
                jobSeekerStatus,
                verificationStatus: null,
                RoleNames.JobSeeker);

        var employer =
            await CreateUserWithRoleAsync(
                environment,
                $"employer-{Guid.NewGuid()}@example.com",
                "Test Employer",
                AccountStatus.Active,
                employerVerificationStatus,
                RoleNames.Employer);

        var skill =
            new Skill(
                Guid.NewGuid(),
                "C#");

        environment.Context.Skills.Add(
            skill);

        var jobSeekerProfile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                jobSeeker.Id,
                Utc(8));

        if (profileReady)
        {
            jobSeekerProfile.UpdateStructuredProfile(
                totalExperienceMonths: 24,
                educationLevel:
                    EducationLevel.Bachelor,
                preferredLocation:
                    "Colombo",
                updatedAtUtc:
                    Utc(9));
        }

        environment.Context
            .JobSeekerProfiles
            .Add(
                jobSeekerProfile);

        environment.Context
            .JobSeekerSkills
            .Add(
                new JobSeekerSkill(
                    jobSeekerProfile.Id,
                    skill.Id));

        if (includeCv)
        {
            environment.Context
                .CvDocuments
                .Add(
                    CreateCvDocument(
                        jobSeekerProfile.Id));
        }

        var employerProfile =
            CreateEmployerProfile(
                employer.Id);

        environment.Context
            .EmployerProfiles
            .Add(
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
                    Utc(10));

        environment.Context
            .Vacancies
            .Add(
                vacancy);

        environment.Context
            .VacancySkills
            .Add(
                new VacancySkill(
                    vacancy.Id,
                    skill.Id));

        await environment.Context
            .SaveChangesAsync();

        return new SeededScenario(
            jobSeeker.Id,
            jobSeekerProfile.Id,
            vacancy.Id);
    }

    private static async Task<ApplicationUser>
        CreateUserWithRoleAsync(
            TestEnvironment environment,
            string email,
            string displayName,
            AccountStatus accountStatus,
            EmployerVerificationStatus?
                verificationStatus,
            string role)
    {
        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName = displayName,
                AccountStatus = accountStatus,
                EmployerVerificationStatus =
                    verificationStatus,
                TokenVersion = 1,
                CreatedAtUtc = Utc(7),
                UpdatedAtUtc = Utc(7)
            };

        var createResult =
            await environment.UserManager
                .CreateAsync(
                    user);

        Assert.True(
            createResult.Succeeded);

        var roleResult =
            await environment.UserManager
                .AddToRoleAsync(
                    user,
                    role);

        Assert.True(
            roleResult.Succeeded);

        return user;
    }

    private static async Task EnsureRoleAsync(
        TestEnvironment environment,
        string roleName)
    {
        if (await environment.RoleManager
                .RoleExistsAsync(
                    roleName))
        {
            return;
        }

        var result =
            await environment.RoleManager
                .CreateAsync(
                    new IdentityRole<Guid>(
                        roleName));

        Assert.True(
            result.Succeeded);
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
                Utc(8),
            UpdatedAtUtc =
                Utc(8)
        };
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
                Utc(11));
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
                    $"HireSyncJobApplicationTests-{Guid.NewGuid()}"));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<
                HireSyncDbContext>();

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

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole<Guid>>>();

        return new TestEnvironment(
            provider,
            scope,
            context,
            userManager,
            roleManager,
            new FakeClock
            {
                UtcNow =
                    Utc(12)
            });
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

    private sealed record SeededScenario(
        Guid JobSeekerUserId,
        Guid JobSeekerProfileId,
        Guid VacancyId);

    private sealed class FakeClock
        : IClock
    {
        public DateTime UtcNow { get; set; }
    }

    private sealed class TestEnvironment
        : IDisposable
    {
        private readonly ServiceProvider
            _provider;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context,
            UserManager<ApplicationUser>
                userManager,
            RoleManager<IdentityRole<Guid>>
                roleManager,
            FakeClock clock)
        {
            _provider =
                provider;

            Scope =
                scope;

            Context =
                context;

            UserManager =
                userManager;

            RoleManager =
                roleManager;

            Clock =
                clock;
        }

        public IServiceScope Scope { get; }

        public HireSyncDbContext Context { get; }

        public UserManager<ApplicationUser>
            UserManager { get; }

        public RoleManager<IdentityRole<Guid>>
            RoleManager { get; }

        public FakeClock Clock { get; }

        public void Dispose()
        {
            Scope.Dispose();
            _provider.Dispose();
        }
    }
}