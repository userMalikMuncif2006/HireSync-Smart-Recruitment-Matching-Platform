using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using HireSync.Infrastructure.Search;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class VacancySearchServiceTests
{
    [Fact]
    public async Task Search_returns_only_open_vacancies_from_eligible_employers()
    {
        using var environment = CreateEnvironment();

        var eligibleUser =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved);

        var suspendedUser =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Suspended,
                EmployerVerificationStatus.Approved);

        var pendingUser =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Pending);

        var eligibleProfile =
            CreateProfile(
                eligibleUser.Id,
                "Eligible Company",
                "PV 10001");

        var suspendedProfile =
            CreateProfile(
                suspendedUser.Id,
                "Suspended Company",
                "PV 10002");

        var pendingProfile =
            CreateProfile(
                pendingUser.Id,
                "Pending Company",
                "PV 10003");

        environment.Context.EmployerProfiles.AddRange(
            eligibleProfile,
            suspendedProfile,
            pendingProfile);

        var openEligible =
            CreateVacancy(
                eligibleProfile.Id,
                "Eligible Developer",
                publishedHour: 10);

        var closedEligible =
            CreateVacancy(
                eligibleProfile.Id,
                "Closed Developer",
                publishedHour: 11);

        closedEligible.Close(
            Utc(12));

        var suspendedVacancy =
            CreateVacancy(
                suspendedProfile.Id,
                "Suspended Developer",
                publishedHour: 13);

        var pendingVacancy =
            CreateVacancy(
                pendingProfile.Id,
                "Pending Developer",
                publishedHour: 14);

        environment.Context.Vacancies.AddRange(
            openEligible,
            closedEligible,
            suspendedVacancy,
            pendingVacancy);

        await environment.Context.SaveChangesAsync();

        var service =
            new VacancySearchService(
                environment.Context);

        var result =
            await service.SearchOpenVacanciesAsync(
                new SearchVacanciesRequest(),
                CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(
            openEligible.Id,
            result.Items[0].Id);
        Assert.Equal(
            1,
            result.TotalCount);
    }

    [Fact]
    public async Task Search_excludes_incomplete_employer_profile()
    {
        using var environment = CreateEnvironment();

        var user =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved);

        var profile =
            CreateProfile(
                user.Id,
                "Incomplete Company",
                "PV 20001");

        profile.Description = string.Empty;

        environment.Context.EmployerProfiles.Add(
            profile);

        var vacancy =
            CreateVacancy(
                profile.Id,
                "Backend Developer",
                publishedHour: 10);

        environment.Context.Vacancies.Add(
            vacancy);

        await environment.Context.SaveChangesAsync();

        var service =
            new VacancySearchService(
                environment.Context);

        var result =
            await service.SearchOpenVacanciesAsync(
                new SearchVacanciesRequest(),
                CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(
            0,
            result.TotalCount);
    }

    [Fact]
    public async Task Search_query_matches_title()
    {
        using var environment = CreateEnvironment();

        var profile =
            await CreateEligibleProfileAsync(
                environment,
                "Acme Holdings",
                "PV 30001");

        environment.Context.Vacancies.AddRange(
            CreateVacancy(
                profile.Id,
                "Backend Developer",
                publishedHour: 10),

            CreateVacancy(
                profile.Id,
                "Frontend Engineer",
                publishedHour: 11));

        await environment.Context.SaveChangesAsync();

        var service =
            new VacancySearchService(
                environment.Context);

        var result =
            await service.SearchOpenVacanciesAsync(
                new SearchVacanciesRequest(
                    Q: "backend"),
                CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(
            "Backend Developer",
            result.Items[0].Title);
    }

    [Fact]
    public async Task Search_query_matches_company_name()
    {
        using var environment = CreateEnvironment();

        var acme =
            await CreateEligibleProfileAsync(
                environment,
                "Acme Holdings",
                "PV 40001");

        var beta =
            await CreateEligibleProfileAsync(
                environment,
                "Beta Systems",
                "PV 40002");

        environment.Context.Vacancies.AddRange(
            CreateVacancy(
                acme.Id,
                "Software Engineer",
                publishedHour: 10),

            CreateVacancy(
                beta.Id,
                "Software Engineer",
                publishedHour: 11));

        await environment.Context.SaveChangesAsync();

        var service =
            new VacancySearchService(
                environment.Context);

        var result =
            await service.SearchOpenVacanciesAsync(
                new SearchVacanciesRequest(
                    Q: "acme"),
                CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(
            "Acme Holdings",
            result.Items[0].CompanyName);
    }

    [Fact]
    public async Task Search_query_matches_description()
    {
        using var environment = CreateEnvironment();

        var profile =
            await CreateEligibleProfileAsync(
                environment,
                "Acme Holdings",
                "PV 50001");

        var cloudVacancy =
            CreateVacancy(
                profile.Id,
                "Software Engineer",
                publishedHour: 10,
                description:
                    "Build secure cloud platform services for enterprise customers.");

        var mobileVacancy =
            CreateVacancy(
                profile.Id,
                "Application Engineer",
                publishedHour: 11,
                description:
                    "Build reliable mobile application services for customers.");

        environment.Context.Vacancies.AddRange(
            cloudVacancy,
            mobileVacancy);

        await environment.Context.SaveChangesAsync();

        var service =
            new VacancySearchService(
                environment.Context);

        var result =
            await service.SearchOpenVacanciesAsync(
                new SearchVacanciesRequest(
                    Q: "cloud"),
                CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(
            cloudVacancy.Id,
            result.Items[0].Id);
    }

    [Fact]
    public async Task Search_location_uses_canonical_normalization()
    {
        using var environment = CreateEnvironment();

        var profile =
            await CreateEligibleProfileAsync(
                environment,
                "Acme Holdings",
                "PV 60001");

        environment.Context.Vacancies.AddRange(
            CreateVacancy(
                profile.Id,
                "Colombo Developer",
                publishedHour: 10,
                location: "Colombo Central"),

            CreateVacancy(
                profile.Id,
                "Kandy Developer",
                publishedHour: 11,
                location: "Kandy"));

        await environment.Context.SaveChangesAsync();

        var service =
            new VacancySearchService(
                environment.Context);

        var result =
            await service.SearchOpenVacanciesAsync(
                new SearchVacanciesRequest(
                    Location: "  colombo   central  "),
                CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(
            "Colombo Developer",
            result.Items[0].Title);
    }

    [Fact]
    public async Task Search_orders_newest_first_and_paginates()
    {
        using var environment = CreateEnvironment();

        var profile =
            await CreateEligibleProfileAsync(
                environment,
                "Acme Holdings",
                "PV 70001");

        var oldest =
            CreateVacancy(
                profile.Id,
                "Oldest Vacancy",
                publishedHour: 9);

        var middle =
            CreateVacancy(
                profile.Id,
                "Middle Vacancy",
                publishedHour: 10);

        var newest =
            CreateVacancy(
                profile.Id,
                "Newest Vacancy",
                publishedHour: 11);

        environment.Context.Vacancies.AddRange(
            oldest,
            middle,
            newest);

        await environment.Context.SaveChangesAsync();

        var service =
            new VacancySearchService(
                environment.Context);

        var firstPage =
            await service.SearchOpenVacanciesAsync(
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Newest,
                    Page: 1,
                    PageSize: 2),
                CancellationToken.None);

        var secondPage =
            await service.SearchOpenVacanciesAsync(
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Newest,
                    Page: 2,
                    PageSize: 2),
                CancellationToken.None);

        Assert.Equal(
            3,
            firstPage.TotalCount);

        Assert.Equal(
            2,
            firstPage.Items.Count);

        Assert.Equal(
            newest.Id,
            firstPage.Items[0].Id);

        Assert.Equal(
            middle.Id,
            firstPage.Items[1].Id);

        Assert.Single(
            secondPage.Items);

        Assert.Equal(
            oldest.Id,
            secondPage.Items[0].Id);
    }

    [Fact]
    public async Task Search_rejects_match_sort_until_matching_contract_is_integrated()
    {
        using var environment = CreateEnvironment();

        var service =
            new VacancySearchService(
                environment.Context);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                service.SearchOpenVacanciesAsync(
                    new SearchVacanciesRequest(
                        Sort: VacancySearchSort.Match),
                    CancellationToken.None));
    }

    private static Vacancy CreateVacancy(
        Guid employerProfileId,
        string title,
        int publishedHour,
        string location = "Colombo",
        string description =
            "Build and maintain secure software services for business customers.")
    {
        return new Vacancy(
            Guid.NewGuid(),
            employerProfileId,
            title,
            description,
            location,
            24,
            EducationLevel.Bachelor,
            Utc(publishedHour));
    }

    private static async Task<EmployerProfile>
        CreateEligibleProfileAsync(
            TestEnvironment environment,
            string companyName,
            string businessRegistrationNumber)
    {
        var user =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved);

        var profile =
            CreateProfile(
                user.Id,
                companyName,
                businessRegistrationNumber);

        environment.Context.EmployerProfiles.Add(
            profile);

        return profile;
    }

    private static EmployerProfile CreateProfile(
        Guid userId,
        string companyName,
        string businessRegistrationNumber)
    {
        var now =
            Utc(8);

        return new EmployerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyName = companyName,
            NormalizedCompanyName =
                companyName.ToUpperInvariant(),
            Description =
                "A complete Employer company profile.",
            Location = "Colombo",
            NormalizedLocation = "COLOMBO",
            ContactPersonName = "Test Contact",
            ContactPersonDesignation = "HR Manager",
            BusinessRegistrationNumber =
                businessRegistrationNumber,
            NormalizedBusinessRegistrationNumber =
                businessRegistrationNumber.ToUpperInvariant(),
            MobileNumber = "0771234567",
            CompanyWebsite = "https://example.com",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
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
                    $"HireSyncVacancySearchTests-{Guid.NewGuid()}"));

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
            context);
    }

    private static async Task<ApplicationUser>
        CreateEmployerAsync(
            TestEnvironment environment,
            AccountStatus accountStatus,
            EmployerVerificationStatus verificationStatus)
    {
        var userManager =
            environment.Scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var roleManager =
            environment.Scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(
                RoleNames.Employer))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole<Guid>(
                        RoleNames.Employer));

            Assert.True(
                roleResult.Succeeded);
        }

        var email =
            $"employer-{Guid.NewGuid()}@example.com";

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName = "Test Employer",
                AccountStatus = accountStatus,
                EmployerVerificationStatus =
                    verificationStatus,
                TokenVersion = 1,
                CreatedAtUtc = Utc(7),
                UpdatedAtUtc = Utc(7)
            };

        var createResult =
            await userManager.CreateAsync(user);

        Assert.True(
            createResult.Succeeded);

        var roleAssignment =
            await userManager.AddToRoleAsync(
                user,
                RoleNames.Employer);

        Assert.True(
            roleAssignment.Succeeded);

        return user;
    }

    private static DateTime Utc(
        int hour) =>
        new(
            2026,
            9,
            11,
            hour,
            0,
            0,
            DateTimeKind.Utc);

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