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

public sealed class ApplicationStatusServiceTests
{
    [Fact]
    public async Task Genuine_transition_updates_status_and_creates_exactly_one_notification()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(
                environment);

        environment.Clock.UtcNow =
            Utc(16);

        var service =
            CreateService(environment);

        var result =
            await service.UpdateOwnApplicationStatusAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                new UpdateApplicationStatusRequest(
                    ApplicationStatus.UnderReview,
                    seeded.RowVersion),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Application);

        Assert.Equal(
            ApplicationStatus.UnderReview,
            result.Application!.Status);

        Assert.Equal(
            environment.Clock.UtcNow,
            result.Application.UpdatedAtUtc);

        environment.Context.ChangeTracker.Clear();

        var savedApplication =
            await environment.Context.JobApplications
                .SingleAsync(candidate =>
                    candidate.Id ==
                    seeded.ApplicationId);

        var notifications =
            await environment.Context.Notifications
                .Where(candidate =>
                    candidate.JobApplicationId ==
                    seeded.ApplicationId)
                .ToListAsync();

        Assert.Equal(
            ApplicationStatus.UnderReview,
            savedApplication.Status);

        Assert.Equal(
            environment.Clock.UtcNow,
            savedApplication.UpdatedAtUtc);

        var notification =
            Assert.Single(notifications);

        Assert.Equal(
            seeded.JobSeekerUserId,
            notification.RecipientUserId);

        Assert.Equal(
            NotificationType.ApplicationStatusChanged,
            notification.Type);

        Assert.Equal(
            seeded.ApplicationId,
            notification.JobApplicationId);

        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);

        Assert.Equal(
            environment.Clock.UtcNow,
            notification.CreatedAtUtc);
    }

    [Fact]
    public async Task Same_state_is_successful_no_op_and_creates_no_notification()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(
                environment);

        var service =
            CreateService(environment);

        var staleRowVersion =
            new byte[]
            {
                9, 9, 9, 9,
                9, 9, 9, 9
            };

        var result =
            await service.UpdateOwnApplicationStatusAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                new UpdateApplicationStatusRequest(
                    ApplicationStatus.Applied,
                    staleRowVersion),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Application);

        Assert.Equal(
            ApplicationStatus.Applied,
            result.Application!.Status);

        Assert.Equal(
            seeded.OriginalUpdatedAtUtc,
            result.Application.UpdatedAtUtc);

        Assert.Empty(
            await environment.Context.Notifications
                .Where(candidate =>
                    candidate.JobApplicationId ==
                    seeded.ApplicationId)
                .ToListAsync());
    }

    [Fact]
    public async Task Invalid_transition_fails_and_creates_no_notification()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(
                environment,
                initialStatus:
                    ApplicationStatus.Selected);

        var service =
            CreateService(environment);

        var result =
            await service.UpdateOwnApplicationStatusAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                new UpdateApplicationStatusRequest(
                    ApplicationStatus.UnderReview,
                    seeded.RowVersion),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            ApplicationStatusUpdateFailureReason.InvalidTransition,
            result.FailureReason);

        Assert.Empty(
            await environment.Context.Notifications
                .Where(candidate =>
                    candidate.JobApplicationId ==
                    seeded.ApplicationId)
                .ToListAsync());
    }

    [Fact]
    public async Task Foreign_employer_cannot_update_application()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(
                environment);

        var foreignEmployer =
            await CreateEmployerAsync(
                environment,
                AccountStatus.Active,
                EmployerVerificationStatus.Approved);

        var service =
            CreateService(environment);

        var result =
            await service.UpdateOwnApplicationStatusAsync(
                foreignEmployer.Id,
                seeded.ApplicationId,
                new UpdateApplicationStatusRequest(
                    ApplicationStatus.UnderReview,
                    seeded.RowVersion),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            ApplicationStatusUpdateFailureReason.NotFound,
            result.FailureReason);

        Assert.Empty(
            await environment.Context.Notifications
                .Where(candidate =>
                    candidate.JobApplicationId ==
                    seeded.ApplicationId)
                .ToListAsync());
    }

    [Theory]
    [InlineData(
        AccountStatus.Suspended,
        EmployerVerificationStatus.Approved)]
    [InlineData(
        AccountStatus.Active,
        EmployerVerificationStatus.Pending)]
    public async Task Ineligible_employer_cannot_update_application(
        AccountStatus accountStatus,
        EmployerVerificationStatus verificationStatus)
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(
                environment,
                employerAccountStatus:
                    accountStatus,
                employerVerificationStatus:
                    verificationStatus);

        var service =
            CreateService(environment);

        var result =
            await service.UpdateOwnApplicationStatusAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                new UpdateApplicationStatusRequest(
                    ApplicationStatus.UnderReview,
                    seeded.RowVersion),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            ApplicationStatusUpdateFailureReason.NotFound,
            result.FailureReason);

        Assert.Empty(
            await environment.Context.Notifications
                .Where(candidate =>
                    candidate.JobApplicationId ==
                    seeded.ApplicationId)
                .ToListAsync());
    }

    private static ApplicationStatusService CreateService(
        TestEnvironment environment)
    {
        return new ApplicationStatusService(
            environment.Context,
            environment.UserManager,
            environment.Clock);
    }

    private static async Task<SeededApplication>
        SeedApplicationAsync(
            TestEnvironment environment,
            AccountStatus employerAccountStatus =
                AccountStatus.Active,
            EmployerVerificationStatus employerVerificationStatus =
                EmployerVerificationStatus.Approved,
            ApplicationStatus initialStatus =
                ApplicationStatus.Applied)
    {
        var employer =
            await CreateEmployerAsync(
                environment,
                employerAccountStatus,
                employerVerificationStatus);

        var jobSeeker =
            await CreateJobSeekerAsync(
                environment);

        var employerProfile =
            CreateEmployerProfile(
                employer.Id);

        var jobSeekerProfile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                jobSeeker.Id,
                Utc(8));

        var vacancy =
            new Vacancy(
                Guid.NewGuid(),
                employerProfile.Id,
                "Backend Developer",
                "Build and maintain secure backend software services.",
                "Colombo",
                24,
                EducationLevel.Bachelor,
                Utc(9));

        var application =
            new JobApplication(
                Guid.NewGuid(),
                vacancy.Id,
                jobSeekerProfile.Id,
                Utc(10));

        if (initialStatus !=
            ApplicationStatus.Applied)
        {
            application.ChangeStatus(
                initialStatus,
                Utc(11));
        }

        environment.Context.EmployerProfiles.Add(
            employerProfile);

        environment.Context.JobSeekerProfiles.Add(
            jobSeekerProfile);

        environment.Context.Vacancies.Add(
            vacancy);

        environment.Context.JobApplications.Add(
            application);

        var rowVersion =
            new byte[]
            {
                1, 2, 3, 4,
                5, 6, 7, 8
            };

        environment.Context.Entry(application)
            .Property(candidate =>
                candidate.RowVersion)
            .CurrentValue =
                rowVersion;

        await environment.Context.SaveChangesAsync();

        return new SeededApplication(
            employer.Id,
            jobSeeker.Id,
            application.Id,
            rowVersion,
            application.UpdatedAtUtc);
    }

    private static async Task<ApplicationUser>
        CreateEmployerAsync(
            TestEnvironment environment,
            AccountStatus accountStatus,
            EmployerVerificationStatus verificationStatus)
    {
        await EnsureEmployerRoleAsync(
            environment);

        var email =
            $"employer-{Guid.NewGuid()}@example.com";

        var user =
            CreateUser(
                email,
                accountStatus,
                verificationStatus,
                "Test Employer");

        var createResult =
            await environment.UserManager
                .CreateAsync(user);

        Assert.True(
            createResult.Succeeded);

        var roleResult =
            await environment.UserManager
                .AddToRoleAsync(
                    user,
                    RoleNames.Employer);

        Assert.True(
            roleResult.Succeeded);

        return user;
    }

    private static async Task<ApplicationUser>
        CreateJobSeekerAsync(
            TestEnvironment environment)
    {
        var email =
            $"jobseeker-{Guid.NewGuid()}@example.com";

        var user =
            CreateUser(
                email,
                AccountStatus.Active,
                verificationStatus: null,
                "Test Job Seeker");

        var createResult =
            await environment.UserManager
                .CreateAsync(user);

        Assert.True(
            createResult.Succeeded);

        return user;
    }

    private static ApplicationUser CreateUser(
        string email,
        AccountStatus accountStatus,
        EmployerVerificationStatus? verificationStatus,
        string displayName)
    {
        return new ApplicationUser
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
    }

    private static async Task EnsureEmployerRoleAsync(
        TestEnvironment environment)
    {
        if (await environment.RoleManager
                .RoleExistsAsync(
                    RoleNames.Employer))
        {
            return;
        }

        var result =
            await environment.RoleManager
                .CreateAsync(
                    new IdentityRole<Guid>(
                        RoleNames.Employer));

        Assert.True(result.Succeeded);
    }

    private static EmployerProfile CreateEmployerProfile(
        Guid userId)
    {
        var registrationNumber =
            $"PV-{Guid.NewGuid():N}";

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
                registrationNumber,
            NormalizedBusinessRegistrationNumber =
                registrationNumber.ToUpperInvariant(),
            MobileNumber = "0771234567",
            CompanyWebsite = "https://example.com",
            CreatedAtUtc = Utc(8),
            UpdatedAtUtc = Utc(8)
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
                    $"HireSyncApplicationStatusTests-{Guid.NewGuid()}"));

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
            new FakeClock
            {
                UtcNow = Utc(15)
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

    private sealed record SeededApplication(
        Guid EmployerUserId,
        Guid JobSeekerUserId,
        Guid ApplicationId,
        byte[] RowVersion,
        DateTime OriginalUpdatedAtUtc);

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; }
    }

    private sealed class TestEnvironment
        : IDisposable
    {
        private readonly ServiceProvider _provider;

        public TestEnvironment(
            ServiceProvider provider,
            IServiceScope scope,
            HireSyncDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            FakeClock clock)
        {
            _provider = provider;
            Scope = scope;
            Context = context;
            UserManager = userManager;
            RoleManager = roleManager;
            Clock = clock;
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
