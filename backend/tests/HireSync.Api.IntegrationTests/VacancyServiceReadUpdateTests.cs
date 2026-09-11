using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Employer;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class VacancyServiceReadUpdateTests
{
    [Fact]
    public async Task GetOwnVacancies_returns_only_owned_vacancies()
    {
        using var environment = CreateEnvironment();

        var owner = await CreateEmployerAsync(environment);
        var other = await CreateEmployerAsync(environment);

        var ownerProfile =
            CreateProfile(owner.Id, "Owner Company", "PV 10001");

        var otherProfile =
            CreateProfile(other.Id, "Other Company", "PV 10002");

        environment.Context.EmployerProfiles.AddRange(
            ownerProfile,
            otherProfile);

        var first =
            CreateVacancy(
                ownerProfile.Id,
                "Backend Developer");

        var second =
            CreateVacancy(
                ownerProfile.Id,
                "Frontend Developer");

        var foreign =
            CreateVacancy(
                otherProfile.Id,
                "Foreign Vacancy");

        environment.Context.Vacancies.AddRange(
            first,
            second,
            foreign);

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.GetOwnVacanciesAsync(
                owner.Id,
                new EmployerVacancyListRequest(),
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalCount);
        Assert.Equal(2, result.Items.Count);

        Assert.All(
            result.Items,
            item =>
                Assert.Contains(
                    item.Id,
                    new[]
                    {
                        first.Id,
                        second.Id
                    }));
    }

    [Fact]
    public async Task GetOwnVacancies_filters_by_status()
    {
        using var environment = CreateEnvironment();

        var owner = await CreateEmployerAsync(environment);

        var profile =
            CreateProfile(
                owner.Id,
                "Owner Company",
                "PV 20001");

        environment.Context.EmployerProfiles.Add(profile);

        var openVacancy =
            CreateVacancy(
                profile.Id,
                "Open Vacancy");

        var closedVacancy =
            CreateVacancy(
                profile.Id,
                "Closed Vacancy");

        closedVacancy.Close(
            new DateTime(
                2026,
                9,
                11,
                14,
                0,
                0,
                DateTimeKind.Utc));

        environment.Context.Vacancies.AddRange(
            openVacancy,
            closedVacancy);

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.GetOwnVacanciesAsync(
                owner.Id,
                new EmployerVacancyListRequest(
                    Status: VacancyStatus.Closed),
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal(
            closedVacancy.Id,
            result.Items[0].Id);
        Assert.Equal(
            VacancyStatus.Closed,
            result.Items[0].Status);
    }

    [Fact]
    public async Task GetOwnVacancy_returns_detail_with_required_skills()
    {
        using var environment = CreateEnvironment();

        var owner = await CreateEmployerAsync(environment);

        var profile =
            CreateProfile(
                owner.Id,
                "Owner Company",
                "PV 30001");

        environment.Context.EmployerProfiles.Add(profile);

        var csharp =
            new Skill(
                Guid.NewGuid(),
                "C#");

        var sql =
            new Skill(
                Guid.NewGuid(),
                "SQL");

        environment.Context.Skills.AddRange(
            csharp,
            sql);

        var vacancy =
            CreateVacancy(
                profile.Id,
                "Backend Developer");

        environment.Context.Vacancies.Add(vacancy);

        environment.Context.VacancySkills.AddRange(
            new VacancySkill(
                vacancy.Id,
                csharp.Id),
            new VacancySkill(
                vacancy.Id,
                sql.Id));

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.GetOwnVacancyAsync(
                owner.Id,
                vacancy.Id,
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(vacancy.Id, result!.Id);
        Assert.Equal(2, result.RequiredSkills.Count);

        Assert.Contains(
            result.RequiredSkills,
            skill => skill.Id == csharp.Id);

        Assert.Contains(
            result.RequiredSkills,
            skill => skill.Id == sql.Id);
    }

    [Fact]
    public async Task GetOwnVacancy_returns_null_for_foreign_vacancy()
    {
        using var environment = CreateEnvironment();

        var owner = await CreateEmployerAsync(environment);
        var other = await CreateEmployerAsync(environment);

        var ownerProfile =
            CreateProfile(
                owner.Id,
                "Owner Company",
                "PV 40001");

        var otherProfile =
            CreateProfile(
                other.Id,
                "Other Company",
                "PV 40002");

        environment.Context.EmployerProfiles.AddRange(
            ownerProfile,
            otherProfile);

        var vacancy =
            CreateVacancy(
                ownerProfile.Id,
                "Backend Developer");

        environment.Context.Vacancies.Add(vacancy);

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.GetOwnVacancyAsync(
                other.Id,
                vacancy.Id,
                CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOwnVacancy_updates_fields_and_replaces_skills()
    {
        using var environment = CreateEnvironment();

        var owner = await CreateEmployerAsync(environment);

        var profile =
            CreateProfile(
                owner.Id,
                "Owner Company",
                "PV 50001");

        environment.Context.EmployerProfiles.Add(profile);

        var oldSkill =
            new Skill(
                Guid.NewGuid(),
                "C#");

        var newSkill =
            new Skill(
                Guid.NewGuid(),
                "SQL");

        environment.Context.Skills.AddRange(
            oldSkill,
            newSkill);

        var vacancy =
            CreateVacancy(
                profile.Id,
                "Backend Developer");

        environment.Context.Vacancies.Add(vacancy);

        environment.Context.VacancySkills.Add(
            new VacancySkill(
                vacancy.Id,
                oldSkill.Id));

        var rowVersion =
            new byte[]
            {
                1, 2, 3, 4,
                5, 6, 7, 8
            };

        environment.Context.Entry(vacancy)
            .Property(candidate => candidate.RowVersion)
            .CurrentValue = rowVersion;

        await environment.Context.SaveChangesAsync();

        environment.Clock.UtcNow =
            new DateTime(
                2026,
                9,
                11,
                16,
                0,
                0,
                DateTimeKind.Utc);

        var service = CreateService(environment);

        var request =
            new UpdateVacancyRequest(
                "Senior Backend Developer",
                "Design and maintain secure backend services and APIs.",
                "Kandy",
                36,
                EducationLevel.Bachelor,
                new[]
                {
                    newSkill.Id
                },
                rowVersion);

        var result =
            await service.UpdateOwnVacancyAsync(
                owner.Id,
                vacancy.Id,
                request,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Vacancy);

        environment.Context.ChangeTracker.Clear();

        var saved =
            await environment.Context.Vacancies
                .SingleAsync(
                    candidate =>
                        candidate.Id == vacancy.Id);

        var savedSkills =
            await environment.Context.VacancySkills
                .Where(candidate =>
                    candidate.VacancyId == vacancy.Id)
                .ToListAsync();

        Assert.Equal(
            "Senior Backend Developer",
            saved.Title);

        Assert.Equal(
            "Kandy",
            saved.Location);

        Assert.Equal(
            "KANDY",
            saved.NormalizedLocation);

        Assert.Equal(
            36,
            saved.MinimumExperienceMonths);

        Assert.Equal(
            environment.Clock.UtcNow,
            saved.UpdatedAtUtc);

        Assert.Single(savedSkills);

        Assert.Equal(
            newSkill.Id,
            savedSkills[0].SkillId);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_not_found_for_foreign_vacancy()
    {
        using var environment = CreateEnvironment();

        var owner = await CreateEmployerAsync(environment);
        var other = await CreateEmployerAsync(environment);

        var ownerProfile =
            CreateProfile(
                owner.Id,
                "Owner Company",
                "PV 60001");

        var otherProfile =
            CreateProfile(
                other.Id,
                "Other Company",
                "PV 60002");

        environment.Context.EmployerProfiles.AddRange(
            ownerProfile,
            otherProfile);

        var skill =
            new Skill(
                Guid.NewGuid(),
                "C#");

        environment.Context.Skills.Add(skill);

        var vacancy =
            CreateVacancy(
                ownerProfile.Id,
                "Backend Developer");

        environment.Context.Vacancies.Add(vacancy);

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.UpdateOwnVacancyAsync(
                other.Id,
                vacancy.Id,
                CreateUpdateRequest(skill.Id),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyUpdateFailureReason.NotFound,
            result.FailureReason);
    }

    [Fact]
    public async Task UpdateOwnVacancy_rejects_closed_vacancy()
    {
        using var environment = CreateEnvironment();

        var owner = await CreateEmployerAsync(environment);

        var profile =
            CreateProfile(
                owner.Id,
                "Owner Company",
                "PV 70001");

        environment.Context.EmployerProfiles.Add(profile);

        var skill =
            new Skill(
                Guid.NewGuid(),
                "C#");

        environment.Context.Skills.Add(skill);

        var vacancy =
            CreateVacancy(
                profile.Id,
                "Backend Developer");

        vacancy.Close(
            new DateTime(
                2026,
                9,
                11,
                14,
                0,
                0,
                DateTimeKind.Utc));

        environment.Context.Vacancies.Add(vacancy);

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.UpdateOwnVacancyAsync(
                owner.Id,
                vacancy.Id,
                CreateUpdateRequest(skill.Id),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyUpdateFailureReason.Closed,
            result.FailureReason);
    }

    private static VacancyService CreateService(
        TestEnvironment environment)
    {
        return new VacancyService(
            environment.Context,
            environment.UserManager,
            environment.Clock);
    }

    private static Vacancy CreateVacancy(
        Guid employerProfileId,
        string title)
    {
        return new Vacancy(
            Guid.NewGuid(),
            employerProfileId,
            title,
            "Build and maintain secure backend application services.",
            "Colombo",
            24,
            EducationLevel.Bachelor,
            new DateTime(
                2026,
                9,
                11,
                12,
                0,
                0,
                DateTimeKind.Utc));
    }

    private static UpdateVacancyRequest CreateUpdateRequest(
        Guid skillId)
    {
        return new UpdateVacancyRequest(
            "Updated Backend Developer",
            "Design and maintain secure backend services and APIs.",
            "Colombo",
            24,
            EducationLevel.Bachelor,
            new[]
            {
                skillId
            },
            new byte[]
            {
                1, 2, 3, 4,
                5, 6, 7, 8
            });
    }

    private static TestEnvironment CreateEnvironment()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<HireSyncDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"HireSyncVacancyReadUpdateTests-{Guid.NewGuid()}"));

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
            new FakeClock());
    }

    private static async Task<ApplicationUser>
        CreateEmployerAsync(
            TestEnvironment environment)
    {
        if (!await environment.RoleManager.RoleExistsAsync(
                RoleNames.Employer))
        {
            var roleResult =
                await environment.RoleManager.CreateAsync(
                    new IdentityRole<Guid>(
                        RoleNames.Employer));

            Assert.True(
                roleResult.Succeeded);
        }

        var email =
            $"employer-{Guid.NewGuid()}@example.com";

        var now =
            new DateTime(
                2026,
                9,
                11,
                11,
                0,
                0,
                DateTimeKind.Utc);

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName = "Test Employer",
                AccountStatus = AccountStatus.Active,
                EmployerVerificationStatus =
                    EmployerVerificationStatus.Approved,
                TokenVersion = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

        var createResult =
            await environment.UserManager.CreateAsync(user);

        Assert.True(createResult.Succeeded);

        var roleAssignment =
            await environment.UserManager.AddToRoleAsync(
                user,
                RoleNames.Employer);

        Assert.True(roleAssignment.Succeeded);

        return user;
    }

    private static EmployerProfile CreateProfile(
        Guid userId,
        string companyName,
        string businessRegistrationNumber)
    {
        var now =
            new DateTime(
                2026,
                9,
                11,
                11,
                30,
                0,
                DateTimeKind.Utc);

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

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } =
            new(
                2026,
                9,
                11,
                15,
                0,
                0,
                DateTimeKind.Utc);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            FakeClock clock)
        {
            _provider = provider;
            _scope = scope;

            Context = context;
            UserManager = userManager;
            RoleManager = roleManager;
            Clock = clock;
        }

        public HireSyncDbContext Context { get; }

        public UserManager<ApplicationUser> UserManager { get; }

        public RoleManager<IdentityRole<Guid>> RoleManager { get; }

        public FakeClock Clock { get; }

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
        }
    }
}