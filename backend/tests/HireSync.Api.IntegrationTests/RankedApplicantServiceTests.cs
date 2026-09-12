using HireSync.Application.DTOs.EmployerApplications;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Matching;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.EmployerApplications;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class RankedApplicantServiceTests
{
    [Fact]
    public async Task Invalid_request_returns_invalid_input()
    {
        using var environment =
            CreateEnvironment();

        var service =
            CreateService(environment);

        var result =
            await service.GetOwnVacancyApplicantsAsync(
                Guid.Empty,
                Guid.NewGuid(),
                new RankedApplicantListRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            RankedApplicantQueryFailureReason.InvalidInput,
            result.FailureReason);

        Assert.Null(result.Page);
    }

    [Fact]
    public async Task Foreign_vacancy_is_concealed_as_not_found()
    {
        using var environment =
            CreateEnvironment();

        var firstEmployer =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active);

        var secondEmployer =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active);

        var secondVacancy =
            await SeedVacancyAsync(
                environment,
                secondEmployer.Profile);

        var service =
            CreateService(environment);

        var result =
            await service.GetOwnVacancyApplicantsAsync(
                firstEmployer.User.Id,
                secondVacancy.Vacancy.Id,
                new RankedApplicantListRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            RankedApplicantQueryFailureReason.VacancyNotFound,
            result.FailureReason);

        Assert.Null(result.Page);
    }

    [Fact]
    public async Task Suspended_employer_cannot_read_own_vacancy()
    {
        using var environment =
            CreateEnvironment();

        var employer =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Suspended);

        var vacancy =
            await SeedVacancyAsync(
                environment,
                employer.Profile);

        var service =
            CreateService(environment);

        var result =
            await service.GetOwnVacancyApplicantsAsync(
                employer.User.Id,
                vacancy.Vacancy.Id,
                new RankedApplicantListRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            RankedApplicantQueryFailureReason.VacancyNotFound,
            result.FailureReason);
    }

    [Fact]
    public async Task Pending_employer_cannot_read_ranked_applicants()
    {
        using var environment =
            CreateEnvironment();

        var employer =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Pending);

        var vacancy =
            await SeedVacancyAsync(
                environment,
                employer.Profile);

        var service =
            CreateService(environment);

        var result =
            await service.GetOwnVacancyApplicantsAsync(
                employer.User.Id,
                vacancy.Vacancy.Id,
                new RankedApplicantListRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            RankedApplicantQueryFailureReason.VacancyNotFound,
            result.FailureReason);

        Assert.Null(result.Page);
    }

    [Fact]
    public async Task Incomplete_employer_profile_cannot_read_ranked_applicants()
    {
        using var environment =
            CreateEnvironment();

        var employer =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved,
                completeProfile: false);

        var vacancy =
            await SeedVacancyAsync(
                environment,
                employer.Profile);

        var service =
            CreateService(environment);

        var result =
            await service.GetOwnVacancyApplicantsAsync(
                employer.User.Id,
                vacancy.Vacancy.Id,
                new RankedApplicantListRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            RankedApplicantQueryFailureReason.VacancyNotFound,
            result.FailureReason);

        Assert.Null(result.Page);
    }

    [Fact]
    public async Task Closed_vacancy_keeps_existing_applicants_visible()
    {
        using var environment =
            CreateEnvironment();

        var employer =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active);

        var vacancy =
            await SeedVacancyAsync(
                environment,
                employer.Profile);

        var applicant =
            await SeedApplicantAsync(
                environment,
                vacancy,
                "Closed Vacancy Candidate",
                new[]
                {
                    vacancy.FirstSkill.Id,
                    vacancy.SecondSkill.Id
                },
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000101"),
                Utc(10));

        vacancy.Vacancy.Close(
            Utc(15));

        await environment.Context
            .SaveChangesAsync();

        var service =
            CreateService(environment);

        var result =
            await service.GetOwnVacancyApplicantsAsync(
                employer.User.Id,
                vacancy.Vacancy.Id,
                new RankedApplicantListRequest());

        Assert.True(result.Succeeded);

        var page =
            Assert.IsType<RankedApplicantPageDto>(
                result.Page);

        var item =
            Assert.Single(page.Items);

        Assert.Equal(
            applicant.Application.Id,
            item.ApplicationId);

        Assert.Equal(
            100.00m,
            item.Match.TotalScore);
    }

    [Fact]
    public async Task Ranking_filter_pagination_and_contact_state_are_deterministic()
    {
        using var environment =
            CreateEnvironment();

        var employer =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active);

        var vacancy =
            await SeedVacancyAsync(
                environment,
                employer.Profile);

        var fullMatch =
            await SeedApplicantAsync(
                environment,
                vacancy,
                "Full Match",
                new[]
                {
                    vacancy.FirstSkill.Id,
                    vacancy.SecondSkill.Id
                },
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000030"),
                Utc(11));

        var tieLowId =
            await SeedApplicantAsync(
                environment,
                vacancy,
                "Tie Low",
                new[]
                {
                    vacancy.FirstSkill.Id
                },
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000010"),
                Utc(10));

        var tieHighId =
            await SeedApplicantAsync(
                environment,
                vacancy,
                "Tie High",
                new[]
                {
                    vacancy.FirstSkill.Id
                },
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000020"),
                Utc(10));

        environment.Context.ContactRequests.Add(
            new ContactRequest(
                Guid.NewGuid(),
                tieLowId.Application.Id,
                Utc(12)));

        await environment.Context
            .SaveChangesAsync();

        var service =
            CreateService(environment);

        var pageTwoResult =
            await service.GetOwnVacancyApplicantsAsync(
                employer.User.Id,
                vacancy.Vacancy.Id,
                new RankedApplicantListRequest(
                    Page: 2,
                    PageSize: 1));

        Assert.True(pageTwoResult.Succeeded);

        var pageTwo =
            Assert.IsType<RankedApplicantPageDto>(
                pageTwoResult.Page);

        Assert.Equal(
            3,
            pageTwo.TotalCount);

        var secondRanked =
            Assert.Single(pageTwo.Items);

        Assert.Equal(
            2,
            secondRanked.Rank);

        Assert.Equal(
            tieLowId.Application.Id,
            secondRanked.ApplicationId);

        Assert.Equal(
            75.00m,
            secondRanked.Match.TotalScore);

        Assert.Equal(
            ContactRequestStatus.Pending,
            secondRanked.ContactRequestStatus);

        fullMatch.Application.ChangeStatus(
            ApplicationStatus.UnderReview,
            Utc(13));

        await environment.Context
            .SaveChangesAsync();

        var filteredResult =
            await service.GetOwnVacancyApplicantsAsync(
                employer.User.Id,
                vacancy.Vacancy.Id,
                new RankedApplicantListRequest(
                    Status: ApplicationStatus.Applied,
                    Page: 1,
                    PageSize: 1));

        Assert.True(filteredResult.Succeeded);

        var filteredPage =
            Assert.IsType<RankedApplicantPageDto>(
                filteredResult.Page);

        Assert.Equal(
            2,
            filteredPage.TotalCount);

        var firstFiltered =
            Assert.Single(filteredPage.Items);

        Assert.Equal(
            1,
            firstFiltered.Rank);

        Assert.Equal(
            tieLowId.Application.Id,
            firstFiltered.ApplicationId);

        var repeatedResult =
            await service.GetOwnVacancyApplicantsAsync(
                employer.User.Id,
                vacancy.Vacancy.Id,
                new RankedApplicantListRequest(
                    Status: ApplicationStatus.Applied,
                    Page: 1,
                    PageSize: 1));

        var repeatedPage =
            Assert.IsType<RankedApplicantPageDto>(
                repeatedResult.Page);

        var repeatedItem =
            Assert.Single(repeatedPage.Items);

        Assert.Equal(
            firstFiltered.ApplicationId,
            repeatedItem.ApplicationId);

        Assert.Equal(
            firstFiltered.Rank,
            repeatedItem.Rank);

        Assert.Equal(
            firstFiltered.Match.TotalScore,
            repeatedItem.Match.TotalScore);

        Assert.NotEqual(
            tieHighId.Application.Id,
            firstFiltered.ApplicationId);
    }

    [Fact]
    public async Task Invalid_current_matching_data_returns_safe_failure()
    {
        using var environment =
            CreateEnvironment();

        var employer =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active);

        var vacancy =
            await SeedVacancyAsync(
                environment,
                employer.Profile);

        await SeedApplicantAsync(
            environment,
            vacancy,
            "No Skills Candidate",
            Array.Empty<Guid>(),
            Guid.Parse(
                "00000000-0000-0000-0000-000000000201"),
            Utc(10));

        var service =
            CreateService(environment);

        var result =
            await service.GetOwnVacancyApplicantsAsync(
                employer.User.Id,
                vacancy.Vacancy.Id,
                new RankedApplicantListRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            RankedApplicantQueryFailureReason.InvalidMatchingData,
            result.FailureReason);

        Assert.Null(result.Page);
    }

    [Fact]
    public void Ranked_applicant_contract_exposes_no_cv_or_personal_contact_fields()
    {
        var propertyNames =
            typeof(RankedApplicantDto)
                .GetProperties()
                .Select(
                    property =>
                        property.Name)
                .ToArray();

        var forbiddenTokens =
            new[]
            {
                "Cv",
                "Email",
                "Phone",
                "Mobile",
                "FileName",
                "Path",
                "ContentType"
            };

        foreach (var token in forbiddenTokens)
        {
            Assert.False(
                propertyNames.Any(
                    propertyName =>
                        propertyName.Contains(
                            token,
                            StringComparison.OrdinalIgnoreCase)),
                $"RankedApplicantDto must not expose a '{token}' field.");
        }
    }

    private static RankedApplicantService
        CreateService(
            TestEnvironment environment)
    {
        return new RankedApplicantService(
            environment.Context,
            new MatchEngine());
    }

    private static async Task<SeededEmployer>
        SeedEmployerAsync(
            TestEnvironment environment,
            AccountStatus accountStatus,
            EmployerVerificationStatus verificationStatus =
                EmployerVerificationStatus.Approved,
            bool completeProfile = true)
    {
        var user =
            await CreateUserInRoleAsync(
                environment,
                RoleNames.Employer,
                "Employer",
                accountStatus,
                verificationStatus);

        var registrationNumber =
            $"PV-{Guid.NewGuid():N}";

        var profile =
            new EmployerProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName =
                    "Ranked Applicant Company",
                NormalizedCompanyName =
                    "RANKED APPLICANT COMPANY",
                Description =
                    completeProfile
                        ? "A complete Employer company profile."
                        : string.Empty,
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

        environment.Context.EmployerProfiles.Add(
            profile);

        await environment.Context
            .SaveChangesAsync();

        return new SeededEmployer(
            user,
            profile);
    }

    private static async Task<SeededVacancy>
        SeedVacancyAsync(
            TestEnvironment environment,
            EmployerProfile employerProfile)
    {
        var firstSkill =
            new Skill(
                Guid.NewGuid(),
                $"SkillA-{Guid.NewGuid():N}");

        var secondSkill =
            new Skill(
                Guid.NewGuid(),
                $"SkillB-{Guid.NewGuid():N}");

        environment.Context.Skills.AddRange(
            firstSkill,
            secondSkill);

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

        environment.Context.VacancySkills.AddRange(
            new VacancySkill(
                vacancy.Id,
                firstSkill.Id),
            new VacancySkill(
                vacancy.Id,
                secondSkill.Id));

        await environment.Context
            .SaveChangesAsync();

        return new SeededVacancy(
            vacancy,
            firstSkill,
            secondSkill);
    }

    private static async Task<SeededApplicant>
        SeedApplicantAsync(
            TestEnvironment environment,
            SeededVacancy vacancy,
            string displayName,
            IReadOnlyCollection<Guid> skillIds,
            Guid applicationId,
            DateTime appliedAtUtc)
    {
        var user =
            await CreateUserInRoleAsync(
                environment,
                RoleNames.JobSeeker,
                displayName,
                AccountStatus.Active,
                verificationStatus: null);

        var profile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                user.Id,
                Utc(8));

        profile.UpdateStructuredProfile(
            totalExperienceMonths: 12,
            educationLevel:
                EducationLevel.Bachelor,
            preferredLocation:
                "Colombo",
            updatedAtUtc:
                Utc(9));

        environment.Context.JobSeekerProfiles.Add(
            profile);

        foreach (var skillId in skillIds)
        {
            environment.Context.JobSeekerSkills.Add(
                new JobSeekerSkill(
                    profile.Id,
                    skillId));
        }

        var application =
            new JobApplication(
                applicationId,
                vacancy.Vacancy.Id,
                profile.Id,
                appliedAtUtc);

        environment.Context.JobApplications.Add(
            application);

        await environment.Context
            .SaveChangesAsync();

        return new SeededApplicant(
            user,
            profile,
            application);
    }

    private static async Task<ApplicationUser>
        CreateUserInRoleAsync(
            TestEnvironment environment,
            string roleName,
            string displayName,
            AccountStatus accountStatus,
            EmployerVerificationStatus? verificationStatus)
    {
        if (!await environment.RoleManager
                .RoleExistsAsync(
                    roleName))
        {
            var createRoleResult =
                await environment.RoleManager
                    .CreateAsync(
                        new IdentityRole<Guid>(
                            roleName));

            Assert.True(
                createRoleResult.Succeeded);
        }

        var email =
            $"{roleName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

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
                CreatedAtUtc =
                    Utc(7),
                UpdatedAtUtc =
                    Utc(7)
            };

        var createUserResult =
            await environment.UserManager
                .CreateAsync(
                    user);

        Assert.True(
            createUserResult.Succeeded);

        var roleResult =
            await environment.UserManager
                .AddToRoleAsync(
                    user,
                    roleName);

        Assert.True(
            roleResult.Succeeded);

        return user;
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
                    $"HireSyncRankedApplicants-{Guid.NewGuid()}"));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<HireSyncDbContext>();

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
            roleManager);
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

    private sealed record SeededEmployer(
        ApplicationUser User,
        EmployerProfile Profile);

    private sealed record SeededVacancy(
        Vacancy Vacancy,
        Skill FirstSkill,
        Skill SecondSkill);

    private sealed record SeededApplicant(
        ApplicationUser User,
        JobSeekerProfile Profile,
        JobApplication Application);

    private sealed class TestEnvironment
        : IDisposable
    {
        private readonly ServiceProvider
            _provider;

        private readonly IServiceScope
            _scope;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _provider =
                provider;

            _scope =
                scope;

            Context =
                context;

            UserManager =
                userManager;

            RoleManager =
                roleManager;
        }

        public HireSyncDbContext Context { get; }

        public UserManager<ApplicationUser>
            UserManager { get; }

        public RoleManager<IdentityRole<Guid>>
            RoleManager { get; }

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
        }
    }
}