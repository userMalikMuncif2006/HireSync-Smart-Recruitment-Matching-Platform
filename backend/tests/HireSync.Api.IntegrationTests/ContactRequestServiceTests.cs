using HireSync.Application.DTOs.ContactRequests;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.ContactRequests;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class ContactRequestServiceTests
{
    [Fact]
    public async Task Create_creates_pending_request_without_notification()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(environment);

        environment.Clock.UtcNow = Utc(15);

        var service = CreateService(environment);

        var result =
            await service.CreateForOwnApplicationAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.ContactRequest);

        Assert.Equal(
            ContactRequestStatus.Pending,
            result.ContactRequest!.Status);

        Assert.Equal(
            seeded.ApplicationId,
            result.ContactRequest.JobApplicationId);

        Assert.Equal(
            environment.Clock.UtcNow,
            result.ContactRequest.RequestedAtUtc);

        Assert.Null(
            result.ContactRequest.RespondedAtUtc);

        environment.Context.ChangeTracker.Clear();

        var saved =
            await environment.Context.ContactRequests
                .SingleAsync(candidate =>
                    candidate.JobApplicationId ==
                    seeded.ApplicationId);

        Assert.Equal(
            ContactRequestStatus.Pending,
            saved.Status);

        Assert.Empty(
            await environment.Context.Notifications
                .ToListAsync());
    }

    [Fact]
    public async Task Create_rejects_rejected_application()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(
                environment,
                applicationStatus:
                    ApplicationStatus.Rejected);

        var service = CreateService(environment);

        var result =
            await service.CreateForOwnApplicationAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            ContactRequestWriteFailureReason.ApplicationRejected,
            result.FailureReason);

        Assert.Empty(
            await environment.Context.ContactRequests
                .ToListAsync());

        Assert.Empty(
            await environment.Context.Notifications
                .ToListAsync());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Create_requires_both_participants_active(
        bool suspendEmployer,
        bool suspendJobSeeker)
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(
                environment,
                employerStatus:
                    suspendEmployer
                        ? AccountStatus.Suspended
                        : AccountStatus.Active,
                jobSeekerStatus:
                    suspendJobSeeker
                        ? AccountStatus.Suspended
                        : AccountStatus.Active);

        var service = CreateService(environment);

        var result =
            await service.CreateForOwnApplicationAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            ContactRequestWriteFailureReason.ParticipantInactive,
            result.FailureReason);

        Assert.Empty(
            await environment.Context.ContactRequests
                .ToListAsync());
    }

    [Fact]
    public async Task Create_prevents_duplicate_contact_request()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(environment);

        var service = CreateService(environment);

        var first =
            await service.CreateForOwnApplicationAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                CancellationToken.None);

        var second =
            await service.CreateForOwnApplicationAsync(
                seeded.EmployerUserId,
                seeded.ApplicationId,
                CancellationToken.None);

        Assert.True(first.Succeeded);

        Assert.False(second.Succeeded);

        Assert.Equal(
            ContactRequestWriteFailureReason.AlreadyExists,
            second.FailureReason);

        Assert.Single(
            await environment.Context.ContactRequests
                .Where(candidate =>
                    candidate.JobApplicationId ==
                    seeded.ApplicationId)
                .ToListAsync());

        Assert.Empty(
            await environment.Context.Notifications
                .ToListAsync());
    }

    [Theory]
    [InlineData(ContactRequestStatus.Accepted)]
    [InlineData(ContactRequestStatus.Declined)]
    public async Task Respond_allows_pending_to_terminal_status_without_notification(
        ContactRequestStatus requestedStatus)
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(environment);

        var contact =
            await SeedContactRequestAsync(
                environment,
                seeded.ApplicationId);

        environment.Clock.UtcNow = Utc(16);

        var service = CreateService(environment);

        var result =
            await service.RespondToOwnContactRequestAsync(
                seeded.JobSeekerUserId,
                contact.ContactRequestId,
                new RespondContactRequestRequest(
                    requestedStatus,
                    contact.RowVersion),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.ContactRequest);

        Assert.Equal(
            requestedStatus,
            result.ContactRequest!.Status);

        Assert.Equal(
            environment.Clock.UtcNow,
            result.ContactRequest.RespondedAtUtc);

        environment.Context.ChangeTracker.Clear();

        var saved =
            await environment.Context.ContactRequests
                .SingleAsync(candidate =>
                    candidate.Id ==
                    contact.ContactRequestId);

        Assert.Equal(
            requestedStatus,
            saved.Status);

        Assert.Equal(
            environment.Clock.UtcNow,
            saved.RespondedAtUtc);

        Assert.Empty(
            await environment.Context.Notifications
                .ToListAsync());
    }

    [Fact]
    public async Task Foreign_job_seeker_cannot_respond()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(environment);

        var contact =
            await SeedContactRequestAsync(
                environment,
                seeded.ApplicationId);

        var foreignJobSeeker =
            await CreateJobSeekerAsync(
                environment,
                AccountStatus.Active);

        var service = CreateService(environment);

        var result =
            await service.RespondToOwnContactRequestAsync(
                foreignJobSeeker.Id,
                contact.ContactRequestId,
                new RespondContactRequestRequest(
                    ContactRequestStatus.Accepted,
                    contact.RowVersion),
                CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Equal(
            ContactRequestWriteFailureReason.NotFound,
            result.FailureReason);

        Assert.Empty(
            await environment.Context.Notifications
                .ToListAsync());
    }

    [Fact]
    public async Task Terminal_contact_request_cannot_transition_again()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(environment);

        var contact =
            await SeedContactRequestAsync(
                environment,
                seeded.ApplicationId);

        var service = CreateService(environment);

        var first =
            await service.RespondToOwnContactRequestAsync(
                seeded.JobSeekerUserId,
                contact.ContactRequestId,
                new RespondContactRequestRequest(
                    ContactRequestStatus.Accepted,
                    contact.RowVersion),
                CancellationToken.None);

        Assert.True(first.Succeeded);

        var second =
            await service.RespondToOwnContactRequestAsync(
                seeded.JobSeekerUserId,
                contact.ContactRequestId,
                new RespondContactRequestRequest(
                    ContactRequestStatus.Declined,
                    contact.RowVersion),
                CancellationToken.None);

        Assert.False(second.Succeeded);

        Assert.Equal(
            ContactRequestWriteFailureReason.InvalidTransition,
            second.FailureReason);

        Assert.Empty(
            await environment.Context.Notifications
                .ToListAsync());
    }

    [Fact]
    public async Task Existing_pending_request_survives_vacancy_closure_and_application_rejection()
    {
        using var environment = CreateEnvironment();

        var seeded =
            await SeedApplicationAsync(environment);

        var contact =
            await SeedContactRequestAsync(
                environment,
                seeded.ApplicationId);

        var application =
            await environment.Context.JobApplications
                .SingleAsync(candidate =>
                    candidate.Id ==
                    seeded.ApplicationId);

        var vacancy =
            await environment.Context.Vacancies
                .SingleAsync(candidate =>
                    candidate.Id ==
                    seeded.VacancyId);

        application.ChangeStatus(
            ApplicationStatus.Rejected,
            Utc(13));

        vacancy.Close(Utc(13));

        await environment.Context.SaveChangesAsync();

        environment.Clock.UtcNow = Utc(16);

        var service = CreateService(environment);

        var result =
            await service.RespondToOwnContactRequestAsync(
                seeded.JobSeekerUserId,
                contact.ContactRequestId,
                new RespondContactRequestRequest(
                    ContactRequestStatus.Accepted,
                    contact.RowVersion),
                CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.ContactRequest);

        Assert.Equal(
            ContactRequestStatus.Accepted,
            result.ContactRequest!.Status);

        Assert.Empty(
            await environment.Context.Notifications
                .ToListAsync());
    }

    private static ContactRequestService CreateService(
        TestEnvironment environment)
    {
        return new ContactRequestService(
            environment.Context,
            environment.UserManager,
            environment.Clock);
    }

    private static async Task<SeededApplication>
        SeedApplicationAsync(
            TestEnvironment environment,
            AccountStatus employerStatus =
                AccountStatus.Active,
            AccountStatus jobSeekerStatus =
                AccountStatus.Active,
            ApplicationStatus applicationStatus =
                ApplicationStatus.Applied)
    {
        var employer =
            await CreateEmployerAsync(
                environment,
                employerStatus);

        var jobSeeker =
            await CreateJobSeekerAsync(
                environment,
                jobSeekerStatus);

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

        if (applicationStatus !=
            ApplicationStatus.Applied)
        {
            application.ChangeStatus(
                applicationStatus,
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

        await environment.Context.SaveChangesAsync();

        return new SeededApplication(
            employer.Id,
            jobSeeker.Id,
            vacancy.Id,
            application.Id);
    }

    private static async Task<SeededContactRequest>
        SeedContactRequestAsync(
            TestEnvironment environment,
            Guid applicationId)
    {
        var contactRequest =
            new ContactRequest(
                Guid.NewGuid(),
                applicationId,
                Utc(12));

        environment.Context.ContactRequests.Add(
            contactRequest);

        var rowVersion =
            new byte[]
            {
                1, 2, 3, 4,
                5, 6, 7, 8
            };

        environment.Context.Entry(contactRequest)
            .Property(candidate =>
                candidate.RowVersion)
            .CurrentValue =
                rowVersion;

        await environment.Context.SaveChangesAsync();

        return new SeededContactRequest(
            contactRequest.Id,
            rowVersion);
    }

    private static async Task<ApplicationUser>
        CreateEmployerAsync(
            TestEnvironment environment,
            AccountStatus status)
    {
        await EnsureRoleAsync(
            environment,
            RoleNames.Employer);

        var email =
            $"employer-{Guid.NewGuid()}@example.com";

        var user =
            CreateUser(
                email,
                "Test Employer",
                status,
                EmployerVerificationStatus.Approved);

        var create =
            await environment.UserManager
                .CreateAsync(user);

        Assert.True(create.Succeeded);

        var role =
            await environment.UserManager
                .AddToRoleAsync(
                    user,
                    RoleNames.Employer);

        Assert.True(role.Succeeded);

        return user;
    }

    private static async Task<ApplicationUser>
        CreateJobSeekerAsync(
            TestEnvironment environment,
            AccountStatus status)
    {
        await EnsureRoleAsync(
            environment,
            RoleNames.JobSeeker);

        var email =
            $"jobseeker-{Guid.NewGuid()}@example.com";

        var user =
            CreateUser(
                email,
                "Test Job Seeker",
                status,
                verificationStatus: null);

        var create =
            await environment.UserManager
                .CreateAsync(user);

        Assert.True(create.Succeeded);

        var role =
            await environment.UserManager
                .AddToRoleAsync(
                    user,
                    RoleNames.JobSeeker);

        Assert.True(role.Succeeded);

        return user;
    }

    private static ApplicationUser CreateUser(
        string email,
        string displayName,
        AccountStatus status,
        EmployerVerificationStatus? verificationStatus)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            AccountStatus = status,
            EmployerVerificationStatus =
                verificationStatus,
            TokenVersion = 1,
            CreatedAtUtc = Utc(7),
            UpdatedAtUtc = Utc(7)
        };
    }

    private static async Task EnsureRoleAsync(
        TestEnvironment environment,
        string roleName)
    {
        if (await environment.RoleManager
                .RoleExistsAsync(roleName))
        {
            return;
        }

        var result =
            await environment.RoleManager
                .CreateAsync(
                    new IdentityRole<Guid>(
                        roleName));

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
            CompanyName = "Contact Test Company",
            NormalizedCompanyName =
                "CONTACT TEST COMPANY",
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
                    $"HireSyncContactRequestTests-{Guid.NewGuid()}"));

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
                UtcNow = Utc(14)
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
        Guid VacancyId,
        Guid ApplicationId);

    private sealed record SeededContactRequest(
        Guid ContactRequestId,
        byte[] RowVersion);

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
