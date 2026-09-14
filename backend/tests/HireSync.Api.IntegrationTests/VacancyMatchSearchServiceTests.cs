using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Matching;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using HireSync.Infrastructure.Search;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class VacancyMatchSearchServiceTests
{
    [Fact]
    public async Task Non_match_sort_returns_invalid_input()
    {
        using var environment = CreateEnvironment();

        var service = CreateService(environment);

        var result =
            await service.SearchOpenVacanciesByMatchAsync(
                Guid.NewGuid(),
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Newest));

        Assert.False(result.Succeeded);
        Assert.Equal(
            VacancySearchFailureReason.InvalidInput,
            result.FailureReason);
        Assert.Null(result.Page);
    }

    [Fact]
    public async Task Suspended_job_seeker_is_unavailable()
    {
        using var environment = CreateEnvironment();

        var user =
            await CreateUserInRoleAsync(
                environment,
                RoleNames.JobSeeker,
                AccountStatus.Suspended,
                null);

        var result =
            await CreateService(environment)
                .SearchOpenVacanciesByMatchAsync(
                    user.Id,
                    new SearchVacanciesRequest(
                        Sort: VacancySearchSort.Match));

        Assert.False(result.Succeeded);
        Assert.Equal(
            VacancySearchFailureReason.JobSeekerUnavailable,
            result.FailureReason);
    }

    [Fact]
    public async Task Wrong_role_is_unavailable()
    {
        using var environment = CreateEnvironment();

        var user =
            await CreateUserInRoleAsync(
                environment,
                RoleNames.Employer,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved);

        var result =
            await CreateService(environment)
                .SearchOpenVacanciesByMatchAsync(
                    user.Id,
                    new SearchVacanciesRequest(
                        Sort: VacancySearchSort.Match));

        Assert.False(result.Succeeded);
        Assert.Equal(
            VacancySearchFailureReason.JobSeekerUnavailable,
            result.FailureReason);
    }

    [Fact]
    public async Task Incomplete_profile_returns_profile_not_ready()
    {
        using var environment = CreateEnvironment();

        var user =
            await CreateUserInRoleAsync(
                environment,
                RoleNames.JobSeeker,
                AccountStatus.Active,
                null);

        environment.Context.JobSeekerProfiles.Add(
            new JobSeekerProfile(
                Guid.NewGuid(),
                user.Id,
                Utc(7)));

        await environment.Context.SaveChangesAsync();

        var result =
            await CreateService(environment)
                .SearchOpenVacanciesByMatchAsync(
                    user.Id,
                    new SearchVacanciesRequest(
                        Sort: VacancySearchSort.Match));

        Assert.False(result.Succeeded);
        Assert.Equal(
            VacancySearchFailureReason.ProfileNotReady,
            result.FailureReason);
    }

    [Fact]
    public async Task Match_sort_scores_orders_and_paginates_without_cv()
    {
        using var environment = CreateEnvironment();

        var skills =
            await SeedSkillsAsync(environment);

        var jobSeeker =
            await SeedReadyJobSeekerAsync(
                environment,
                new[]
                {
                    skills.A,
                    skills.B
                });

        var employer =
            await SeedEmployerAsync(environment);

        var full =
            await SeedVacancyAsync(
                environment,
                employer,
                new[]
                {
                    skills.A,
                    skills.B
                },
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000040"),
                "Full Match",
                "Colombo",
                10);

        var tieLow =
            await SeedVacancyAsync(
                environment,
                employer,
                new[]
                {
                    skills.A,
                    skills.C
                },
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000010"),
                "Tie Low",
                "Colombo",
                11);

        var tieHigh =
            await SeedVacancyAsync(
                environment,
                employer,
                new[]
                {
                    skills.A,
                    skills.C
                },
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000020"),
                "Tie High",
                "Colombo",
                11);

        var lower =
            await SeedVacancyAsync(
                environment,
                employer,
                new[]
                {
                    skills.C
                },
                Guid.Parse(
                    "00000000-0000-0000-0000-000000000030"),
                "Lower Match",
                "Colombo",
                12);

        Assert.False(
            await environment.Context.CvDocuments.AnyAsync());

        var service =
            CreateService(environment);

        var firstResult =
            await service.SearchOpenVacanciesByMatchAsync(
                jobSeeker.User.Id,
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Match,
                    Page: 1,
                    PageSize: 3));

        Assert.True(firstResult.Succeeded);

        var firstPage =
            Assert.IsType<PublicVacancyPageDto>(
                firstResult.Page);

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(3, firstPage.Items.Count);

        Assert.Equal(
            full.Id,
            firstPage.Items[0].Id);
        Assert.Equal(
            100.00m,
            firstPage.Items[0].MatchScore!.Value);

        Assert.Equal(
            tieLow.Id,
            firstPage.Items[1].Id);
        Assert.Equal(
            75.00m,
            firstPage.Items[1].MatchScore!.Value);

        Assert.Equal(
            tieHigh.Id,
            firstPage.Items[2].Id);
        Assert.Equal(
            75.00m,
            firstPage.Items[2].MatchScore!.Value);

        var secondResult =
            await service.SearchOpenVacanciesByMatchAsync(
                jobSeeker.User.Id,
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Match,
                    Page: 2,
                    PageSize: 3));

        var secondPage =
            Assert.IsType<PublicVacancyPageDto>(
                secondResult.Page);

        var last =
            Assert.Single(secondPage.Items);

        Assert.Equal(
            lower.Id,
            last.Id);
        Assert.Equal(
            50.00m,
            last.MatchScore!.Value);

        var repeated =
            await service.SearchOpenVacanciesByMatchAsync(
                jobSeeker.User.Id,
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Match,
                    Page: 1,
                    PageSize: 3));

        var repeatedPage =
            Assert.IsType<PublicVacancyPageDto>(
                repeated.Page);

        Assert.Equal(
            firstPage.Items
                .Select(item => item.Id)
                .ToArray(),
            repeatedPage.Items
                .Select(item => item.Id)
                .ToArray());

        Assert.Equal(
            firstPage.Items
                .Select(item => item.MatchScore)
                .ToArray(),
            repeatedPage.Items
                .Select(item => item.MatchScore)
                .ToArray());
    }

    [Fact]
    public async Task Keyword_and_location_filters_apply_before_ranking()
    {
        using var environment = CreateEnvironment();

        var skills =
            await SeedSkillsAsync(environment);

        var jobSeeker =
            await SeedReadyJobSeekerAsync(
                environment,
                new[] { skills.A });

        var employer =
            await SeedEmployerAsync(environment);

        var expected =
            await SeedVacancyAsync(
                environment,
                employer,
                new[] { skills.A },
                Guid.NewGuid(),
                "Backend Developer",
                "Colombo",
                10);

        await SeedVacancyAsync(
            environment,
            employer,
            new[] { skills.A },
            Guid.NewGuid(),
            "Frontend Developer",
            "Kandy",
            11);

        var result =
            await CreateService(environment)
                .SearchOpenVacanciesByMatchAsync(
                    jobSeeker.User.Id,
                    new SearchVacanciesRequest(
                        Q: "backend",
                        Location: "  colombo  ",
                        Sort: VacancySearchSort.Match));

        Assert.True(result.Succeeded);

        var page =
            Assert.IsType<PublicVacancyPageDto>(
                result.Page);

        Assert.Equal(1, page.TotalCount);

        var item =
            Assert.Single(page.Items);

        Assert.Equal(
            expected.Id,
            item.Id);
    }

    [Fact]
    public async Task Match_search_excludes_ineligible_vacancies()
    {
        using var environment = CreateEnvironment();

        var skills =
            await SeedSkillsAsync(environment);

        var jobSeeker =
            await SeedReadyJobSeekerAsync(
                environment,
                new[] { skills.A });

        var eligible =
            await SeedEmployerAsync(environment);

        var suspended =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Suspended,
                EmployerVerificationStatus.Approved);

        var pending =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Pending);

        var incomplete =
            await SeedEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved,
                completeProfile: false);

        var expected =
            await SeedVacancyAsync(
                environment,
                eligible,
                new[] { skills.A },
                Guid.NewGuid(),
                "Eligible Vacancy",
                "Colombo",
                10);

        var closed =
            await SeedVacancyAsync(
                environment,
                eligible,
                new[] { skills.A },
                Guid.NewGuid(),
                "Closed Vacancy",
                "Colombo",
                11);

        closed.Close(
            Utc(12));

        await SeedVacancyAsync(
            environment,
            suspended,
            new[] { skills.A },
            Guid.NewGuid(),
            "Suspended Employer",
            "Colombo",
            13);

        await SeedVacancyAsync(
            environment,
            pending,
            new[] { skills.A },
            Guid.NewGuid(),
            "Pending Employer",
            "Colombo",
            14);

        await SeedVacancyAsync(
            environment,
            incomplete,
            new[] { skills.A },
            Guid.NewGuid(),
            "Incomplete Employer",
            "Colombo",
            15);

        await environment.Context.SaveChangesAsync();

        var result =
            await CreateService(environment)
                .SearchOpenVacanciesByMatchAsync(
                    jobSeeker.User.Id,
                    new SearchVacanciesRequest(
                        Sort: VacancySearchSort.Match));

        Assert.True(result.Succeeded);

        var page =
            Assert.IsType<PublicVacancyPageDto>(
                result.Page);

        Assert.Equal(1, page.TotalCount);

        var item =
            Assert.Single(page.Items);

        Assert.Equal(
            expected.Id,
            item.Id);
    }

    [Fact]
    public async Task Missing_required_skills_returns_safe_failure()
    {
        using var environment = CreateEnvironment();

        var skills =
            await SeedSkillsAsync(environment);

        var jobSeeker =
            await SeedReadyJobSeekerAsync(
                environment,
                new[] { skills.A });

        var employer =
            await SeedEmployerAsync(environment);

        await SeedVacancyAsync(
            environment,
            employer,
            Array.Empty<Skill>(),
            Guid.NewGuid(),
            "Malformed Vacancy",
            "Colombo",
            10);

        var result =
            await CreateService(environment)
                .SearchOpenVacanciesByMatchAsync(
                    jobSeeker.User.Id,
                    new SearchVacanciesRequest(
                        Sort: VacancySearchSort.Match));

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancySearchFailureReason.InvalidMatchingData,
            result.FailureReason);

        Assert.Null(result.Page);
    }

    [Fact]
    public async Task Newest_search_leaves_match_score_null()
    {
        using var environment = CreateEnvironment();

        var skills =
            await SeedSkillsAsync(environment);

        var employer =
            await SeedEmployerAsync(environment);

        await SeedVacancyAsync(
            environment,
            employer,
            new[] { skills.A },
            Guid.NewGuid(),
            "Newest Search Vacancy",
            "Colombo",
            10);

        var result =
            await new VacancySearchService(
                    environment.Context)
                .SearchOpenVacanciesAsync(
                    new SearchVacanciesRequest(
                        Sort: VacancySearchSort.Newest));

        var item =
            Assert.Single(result.Items);

        Assert.Null(
            item.MatchScore);
    }

    private static VacancyMatchSearchService
        CreateService(
            TestEnvironment environment)
    {
        return new VacancyMatchSearchService(
            environment.Context,
            new MatchEngine());
    }

    private static async Task<SeededSkills>
        SeedSkillsAsync(
            TestEnvironment environment)
    {
        var a =
            new Skill(
                Guid.NewGuid(),
                "CSharp");

        var b =
            new Skill(
                Guid.NewGuid(),
                "SQL");

        var c =
            new Skill(
                Guid.NewGuid(),
                "Azure");

        environment.Context.Skills.AddRange(
            a,
            b,
            c);

        await environment.Context.SaveChangesAsync();

        return new SeededSkills(
            a,
            b,
            c);
    }

    private static async Task<SeededJobSeeker>
        SeedReadyJobSeekerAsync(
            TestEnvironment environment,
            IReadOnlyCollection<Skill> skills)
    {
        var user =
            await CreateUserInRoleAsync(
                environment,
                RoleNames.JobSeeker,
                AccountStatus.Active,
                null);

        var profile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                user.Id,
                Utc(7));

        profile.UpdateStructuredProfile(
            totalExperienceMonths: 24,
            educationLevel:
                EducationLevel.Bachelor,
            preferredLocation:
                "Colombo",
            updatedAtUtc:
                Utc(8));

        environment.Context.JobSeekerProfiles.Add(
            profile);

        foreach (var skill in skills)
        {
            environment.Context.JobSeekerSkills.Add(
                new JobSeekerSkill(
                    profile.Id,
                    skill.Id));
        }

        await environment.Context.SaveChangesAsync();

        return new SeededJobSeeker(
            user,
            profile);
    }

    private static async Task<EmployerProfile>
        SeedEmployerAsync(
            TestEnvironment environment,
            AccountStatus accountStatus =
                AccountStatus.Active,
            EmployerVerificationStatus verificationStatus =
                EmployerVerificationStatus.Approved,
            bool completeProfile = true)
    {
        var user =
            await CreateUserInRoleAsync(
                environment,
                RoleNames.Employer,
                accountStatus,
                verificationStatus);

        var registration =
            $"PV-{Guid.NewGuid():N}";

        var profile =
            new EmployerProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName =
                    $"Company-{Guid.NewGuid():N}",
                NormalizedCompanyName =
                    $"COMPANY-{Guid.NewGuid():N}",
                Description =
                    completeProfile
                        ? "A complete Employer profile for vacancy search testing."
                        : string.Empty,
                Location = "Colombo",
                NormalizedLocation = "COLOMBO",
                ContactPersonName = "Test Contact",
                ContactPersonDesignation = "HR Manager",
                BusinessRegistrationNumber =
                    registration,
                NormalizedBusinessRegistrationNumber =
                    registration.ToUpperInvariant(),
                MobileNumber = "0771234567",
                CompanyWebsite =
                    "https://example.com",
                CreatedAtUtc = Utc(7),
                UpdatedAtUtc = Utc(7)
            };

        environment.Context.EmployerProfiles.Add(
            profile);

        await environment.Context.SaveChangesAsync();

        return profile;
    }

    private static async Task<Vacancy>
        SeedVacancyAsync(
            TestEnvironment environment,
            EmployerProfile employerProfile,
            IReadOnlyCollection<Skill> requiredSkills,
            Guid vacancyId,
            string title,
            string location,
            int publishedHour)
    {
        var vacancy =
            new Vacancy(
                vacancyId,
                employerProfile.Id,
                title,
                "Build and maintain secure production software services.",
                location,
                minimumExperienceMonths: 24,
                requiredEducationLevel:
                    EducationLevel.Bachelor,
                publishedAtUtc:
                    Utc(publishedHour));

        environment.Context.Vacancies.Add(
            vacancy);

        foreach (var skill in requiredSkills)
        {
            environment.Context.VacancySkills.Add(
                new VacancySkill(
                    vacancy.Id,
                    skill.Id));
        }

        await environment.Context.SaveChangesAsync();

        return vacancy;
    }

    private static async Task<ApplicationUser>
        CreateUserInRoleAsync(
            TestEnvironment environment,
            string roleName,
            AccountStatus accountStatus,
            EmployerVerificationStatus?
                verificationStatus)
    {
        if (!await environment.RoleManager
                .RoleExistsAsync(roleName))
        {
            var roleResult =
                await environment.RoleManager
                    .CreateAsync(
                        new IdentityRole<Guid>(
                            roleName));

            Assert.True(
                roleResult.Succeeded);
        }

        var email =
            $"{roleName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName =
                    $"{roleName} Test User",
                AccountStatus =
                    accountStatus,
                EmployerVerificationStatus =
                    verificationStatus,
                TokenVersion = 1,
                CreatedAtUtc = Utc(6),
                UpdatedAtUtc = Utc(6)
            };

        var createResult =
            await environment.UserManager
                .CreateAsync(user);

        Assert.True(
            createResult.Succeeded);

        var roleAssignmentResult =
            await environment.UserManager
                .AddToRoleAsync(
                    user,
                    roleName);

        Assert.True(
            roleAssignmentResult.Succeeded);

        return user;
    }

    private static TestEnvironment
        CreateEnvironment()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<HireSyncDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"HireSyncMatchSearch-{Guid.NewGuid()}"));

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
                .GetRequiredService<HireSyncDbContext>();

        context.Database.EnsureCreated();

        return new TestEnvironment(
            provider,
            scope,
            context,
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>(),
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole<Guid>>>());
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

    private sealed record SeededSkills(
        Skill A,
        Skill B,
        Skill C);

    private sealed record SeededJobSeeker(
        ApplicationUser User,
        JobSeekerProfile Profile);

    private sealed class TestEnvironment
        : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _provider = provider;
            _scope = scope;
            Context = context;
            UserManager = userManager;
            RoleManager = roleManager;
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