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

public sealed class VacancyServiceTests
{
    [Fact]
    public async Task CreateOwnVacancy_creates_vacancy_and_required_skills()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            AccountStatus.Active,
            EmployerVerificationStatus.Approved);

        var profile = CreateCompleteProfile(user.Id);

        environment.Context.EmployerProfiles.Add(profile);

        var csharp = new Skill(
            Guid.NewGuid(),
            "C#");

        var sql = new Skill(
            Guid.NewGuid(),
            "SQL");

        environment.Context.Skills.AddRange(
            csharp,
            sql);

        await environment.Context.SaveChangesAsync();

        environment.Clock.UtcNow =
            new DateTime(
                2026,
                9,
                11,
                14,
                0,
                0,
                DateTimeKind.Utc);

        var request =
            new CreateVacancyRequest(
                "Backend Developer",
                "Build and maintain secure backend application services.",
                "  Colombo   Central ",
                24,
                EducationLevel.Bachelor,
                new[]
                {
                    csharp.Id,
                    sql.Id
                });

        var service = CreateService(environment);

        var result =
            await service.CreateOwnVacancyAsync(
                user.Id,
                request,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Vacancy);

        environment.Context.ChangeTracker.Clear();

        var vacancy =
            await environment.Context.Vacancies
                .SingleAsync();

        var vacancySkills =
            await environment.Context.VacancySkills
                .Where(candidate =>
                    candidate.VacancyId == vacancy.Id)
                .ToListAsync();

        Assert.Equal(
            profile.Id,
            vacancy.EmployerProfileId);

        Assert.Equal(
            "Backend Developer",
            vacancy.Title);

        Assert.Equal(
            "Colombo Central",
            vacancy.Location);

        Assert.Equal(
            "COLOMBO CENTRAL",
            vacancy.NormalizedLocation);

        Assert.Equal(
            24,
            vacancy.MinimumExperienceMonths);

        Assert.Equal(
            EducationLevel.Bachelor,
            vacancy.RequiredEducationLevel);

        Assert.Equal(
            VacancyStatus.Open,
            vacancy.Status);

        Assert.Equal(
            environment.Clock.UtcNow,
            vacancy.PublishedAtUtc);

        Assert.Equal(
            environment.Clock.UtcNow,
            vacancy.UpdatedAtUtc);

        Assert.Null(vacancy.ClosedAtUtc);

        Assert.Equal(
            2,
            vacancySkills.Count);

        Assert.Contains(
            vacancySkills,
            candidate =>
                candidate.SkillId == csharp.Id);

        Assert.Contains(
            vacancySkills,
            candidate =>
                candidate.SkillId == sql.Id);

        Assert.Equal(
            2,
            result.Vacancy!.RequiredSkills.Count);
    }

    [Fact]
    public async Task CreateOwnVacancy_rejects_employer_when_not_approved()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            AccountStatus.Active,
            EmployerVerificationStatus.Pending);

        environment.Context.EmployerProfiles.Add(
            CreateCompleteProfile(user.Id));

        var skill = new Skill(
            Guid.NewGuid(),
            "C#");

        environment.Context.Skills.Add(skill);

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.CreateOwnVacancyAsync(
                user.Id,
                CreateValidRequest(skill.Id),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyCreateFailureReason.EmployerNotReady,
            result.FailureReason);

        Assert.Empty(
            await environment.Context.Vacancies
                .ToListAsync());
    }

    [Fact]
    public async Task CreateOwnVacancy_rejects_suspended_employer()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            AccountStatus.Suspended,
            EmployerVerificationStatus.Approved);

        environment.Context.EmployerProfiles.Add(
            CreateCompleteProfile(user.Id));

        var skill = new Skill(
            Guid.NewGuid(),
            "C#");

        environment.Context.Skills.Add(skill);

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.CreateOwnVacancyAsync(
                user.Id,
                CreateValidRequest(skill.Id),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyCreateFailureReason.EmployerNotReady,
            result.FailureReason);
    }

    [Fact]
    public async Task CreateOwnVacancy_rejects_duplicate_required_skill_ids()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            AccountStatus.Active,
            EmployerVerificationStatus.Approved);

        environment.Context.EmployerProfiles.Add(
            CreateCompleteProfile(user.Id));

        var skill = new Skill(
            Guid.NewGuid(),
            "C#");

        environment.Context.Skills.Add(skill);

        await environment.Context.SaveChangesAsync();

        var request =
            new CreateVacancyRequest(
                "Backend Developer",
                "Build and maintain secure backend application services.",
                "Colombo",
                12,
                EducationLevel.Bachelor,
                new[]
                {
                    skill.Id,
                    skill.Id
                });

        var service = CreateService(environment);

        var result =
            await service.CreateOwnVacancyAsync(
                user.Id,
                request,
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyCreateFailureReason.InvalidInput,
            result.FailureReason);
    }

    [Fact]
    public async Task CreateOwnVacancy_rejects_unknown_skill_id()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            AccountStatus.Active,
            EmployerVerificationStatus.Approved);

        environment.Context.EmployerProfiles.Add(
            CreateCompleteProfile(user.Id));

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.CreateOwnVacancyAsync(
                user.Id,
                CreateValidRequest(Guid.NewGuid()),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyCreateFailureReason.InvalidRequiredSkills,
            result.FailureReason);
    }

    [Fact]
    public async Task CreateOwnVacancy_rejects_incomplete_employer_profile()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            AccountStatus.Active,
            EmployerVerificationStatus.Approved);

        var profile =
            CreateCompleteProfile(user.Id);

        profile.Description = string.Empty;

        environment.Context.EmployerProfiles.Add(profile);

        var skill = new Skill(
            Guid.NewGuid(),
            "C#");

        environment.Context.Skills.Add(skill);

        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result =
            await service.CreateOwnVacancyAsync(
                user.Id,
                CreateValidRequest(skill.Id),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyCreateFailureReason.EmployerNotReady,
            result.FailureReason);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_closes_owned_open_vacancy()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            AccountStatus.Active,
            EmployerVerificationStatus.Approved);

        var profile =
            CreateCompleteProfile(user.Id);

        environment.Context.EmployerProfiles.Add(profile);

        var vacancy = CreateVacancy(profile.Id);

        environment.Context.Vacancies.Add(vacancy);

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
                15,
                0,
                0,
                DateTimeKind.Utc);

        var service =
            CreateService(environment);

        var request =
            new UpdateVacancyStatusRequest(
                VacancyStatus.Closed,
                rowVersion);

        var result =
            await service.UpdateOwnVacancyStatusAsync(
                user.Id,
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

        Assert.Equal(
            VacancyStatus.Closed,
            saved.Status);

        Assert.Equal(
            environment.Clock.UtcNow,
            saved.ClosedAtUtc);

        Assert.Equal(
            environment.Clock.UtcNow,
            saved.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_not_found_for_foreign_vacancy()
    {
        using var environment = CreateEnvironment();

        var owner =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved);

        var otherEmployer =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved);

        var ownerProfile =
            CreateCompleteProfile(
                owner.Id,
                "Acme Holdings",
                "PV 12345");

        var otherProfile =
            CreateCompleteProfile(
                otherEmployer.Id,
                "Other Holdings",
                "PV 54321");

        environment.Context.EmployerProfiles.AddRange(
            ownerProfile,
            otherProfile);

        var vacancy =
            CreateVacancy(ownerProfile.Id);

        environment.Context.Vacancies.Add(vacancy);

        await environment.Context.SaveChangesAsync();

        var service =
            CreateService(environment);

        var result =
            await service.UpdateOwnVacancyStatusAsync(
                otherEmployer.Id,
                vacancy.Id,
                new UpdateVacancyStatusRequest(
                    VacancyStatus.Closed,
                    new byte[]
                    {
                        1, 2, 3, 4,
                        5, 6, 7, 8
                    }),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyStatusUpdateFailureReason.NotFound,
            result.FailureReason);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_already_closed_for_closed_vacancy()
    {
        using var environment = CreateEnvironment();

        var user =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved);

        var profile =
            CreateCompleteProfile(user.Id);

        environment.Context.EmployerProfiles.Add(profile);

        var vacancy =
            CreateVacancy(profile.Id);

        vacancy.Close(
            new DateTime(
                2026,
                9,
                11,
                13,
                0,
                0,
                DateTimeKind.Utc));

        environment.Context.Vacancies.Add(vacancy);

        await environment.Context.SaveChangesAsync();

        var service =
            CreateService(environment);

        var result =
            await service.UpdateOwnVacancyStatusAsync(
                user.Id,
                vacancy.Id,
                new UpdateVacancyStatusRequest(
                    VacancyStatus.Closed,
                    new byte[]
                    {
                        1, 2, 3, 4,
                        5, 6, 7, 8
                    }),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            VacancyStatusUpdateFailureReason.AlreadyClosed,
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
        Guid employerProfileId)
    {
        return new Vacancy(
            Guid.NewGuid(),
            employerProfileId,
            "Backend Developer",
            "Build and maintain secure backend application services.",
            "Colombo",
            24,
            EducationLevel.Bachelor,
            new DateTime(
                2026,
                9,
                11,
                12,
                45,
                0,
                DateTimeKind.Utc));
    }

    private static CreateVacancyRequest CreateValidRequest(
        Guid skillId)
    {
        return new CreateVacancyRequest(
            "Backend Developer",
            "Build and maintain secure backend application services.",
            "Colombo",
            24,
            EducationLevel.Bachelor,
            new[]
            {
                skillId
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
                    $"HireSyncVacancyTests-{Guid.NewGuid()}"));

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
            TestEnvironment environment,
            AccountStatus accountStatus,
            EmployerVerificationStatus verificationStatus)
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

        var now =
            new DateTime(
                2026,
                9,
                11,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var email =
            $"employer-{Guid.NewGuid()}@example.com";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = "Test Employer",
            AccountStatus = accountStatus,
            EmployerVerificationStatus =
                verificationStatus,
            TokenVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var createResult =
            await environment.UserManager.CreateAsync(
                user);

        Assert.True(
            createResult.Succeeded);

        var roleAssignment =
            await environment.UserManager.AddToRoleAsync(
                user,
                RoleNames.Employer);

        Assert.True(
            roleAssignment.Succeeded);

        return user;
    }

    private static EmployerProfile CreateCompleteProfile(
        Guid userId,
        string companyName = "Acme Holdings",
        string businessRegistrationNumber = "PV 12345")
    {
        var createdAt =
            new DateTime(
                2026,
                9,
                11,
                12,
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
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt
        };
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } =
            new(
                2026,
                9,
                11,
                13,
                0,
                0,
                DateTimeKind.Utc);
    }

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