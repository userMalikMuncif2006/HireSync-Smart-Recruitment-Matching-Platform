using HireSync.Application.DTOs.Employer;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Domain.Rules;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Employer;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class EmployerProfileServiceTests
{
    [Fact]
    public async Task GetOwnProfile_returns_profile_with_account_projection()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            EmployerVerificationStatus.Approved);

        var profile = CreateProfile(user.Id);

        environment.Context.EmployerProfiles.Add(profile);
        await environment.Context.SaveChangesAsync();

        var service = CreateService(environment);

        var result = await service.GetOwnProfileAsync(
            user.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(profile.Id, result!.Id);
        Assert.Equal(user.Email, result.BusinessEmail);
        Assert.Equal(
            EmployerVerificationStatus.Approved,
            result.EmployerVerificationStatus);
        Assert.True(result.IsProfileComplete);
        Assert.True(result.IsVacancyReady);
    }

    [Fact]
    public async Task GetOwnProfile_returns_null_when_profile_missing()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            EmployerVerificationStatus.Approved);

        var service = CreateService(environment);

        var result = await service.GetOwnProfileAsync(
            user.Id,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOwnProfile_updates_and_normalizes_profile()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            EmployerVerificationStatus.Pending);

        var profile = CreateProfile(user.Id);

        environment.Context.EmployerProfiles.Add(profile);
        await environment.Context.SaveChangesAsync();

        environment.Clock.UtcNow =
            new DateTime(
                2026,
                9,
                11,
                1,
                0,
                0,
                DateTimeKind.Utc);

        var request =
            new UpdateEmployerProfileRequest(
                "  Beta   Holdings (Pvt) Ltd. ",
                "An updated and complete Employer company profile.",
                "  Kandy   Central ",
                "  New Contact  ",
                "  Talent Manager  ",
                " pv   98765/a ",
                " 0712345678 ",
                " https://example.org ");

        var service = CreateService(environment);

        var result = await service.UpdateOwnProfileAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Profile);

        environment.Context.ChangeTracker.Clear();

        var saved = await environment.Context.EmployerProfiles
            .SingleAsync(candidate => candidate.UserId == user.Id);

        Assert.Equal(
            "Beta Holdings (Pvt) Ltd.",
            saved.CompanyName);

        Assert.Equal(
            "BETA HOLDINGS (PVT) LTD.",
            saved.NormalizedCompanyName);

        Assert.Equal(
            "Kandy Central",
            saved.Location);

        Assert.Equal(
            "KANDY CENTRAL",
            saved.NormalizedLocation);

        Assert.Equal(
            "pv 98765/a",
            saved.BusinessRegistrationNumber);

        Assert.Equal(
            "PV 98765/A",
            saved.NormalizedBusinessRegistrationNumber);

        Assert.Equal(
            "New Contact",
            saved.ContactPersonName);

        Assert.Equal(
            "Talent Manager",
            saved.ContactPersonDesignation);

        Assert.Equal(
            "0712345678",
            saved.MobileNumber);

        Assert.Equal(
            "https://example.org",
            saved.CompanyWebsite);

        Assert.Equal(
            environment.Clock.UtcNow,
            saved.UpdatedAtUtc);

        Assert.Equal(
            EmployerVerificationStatus.Pending,
            result.Profile!.EmployerVerificationStatus);

        Assert.False(result.Profile.IsVacancyReady);
    }

    [Fact]
    public async Task UpdateOwnProfile_rejects_invalid_input()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            EmployerVerificationStatus.Pending);

        environment.Context.EmployerProfiles.Add(
            CreateProfile(user.Id));

        await environment.Context.SaveChangesAsync();

        var request =
            new UpdateEmployerProfileRequest(
                "A",
                "Too short",
                "X",
                "A",
                "B",
                "1",
                "1",
                "not-a-url");

        var service = CreateService(environment);

        var result = await service.UpdateOwnProfileAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            EmployerProfileUpdateFailureReason.InvalidInput,
            result.FailureReason);
    }

    [Fact]
    public async Task UpdateOwnProfile_rejects_duplicate_normalized_brn()
    {
        using var environment = CreateEnvironment();

        var firstUser = await CreateEmployerAsync(
            environment,
            EmployerVerificationStatus.Pending,
            "first-employer@example.com");

        var secondUser = await CreateEmployerAsync(
            environment,
            EmployerVerificationStatus.Pending,
            "second-employer@example.com");

        environment.Context.EmployerProfiles.Add(
            CreateProfile(
                firstUser.Id,
                businessRegistrationNumber: "PV 11111"));

        environment.Context.EmployerProfiles.Add(
            CreateProfile(
                secondUser.Id,
                companyName: "Second Holdings",
                businessRegistrationNumber: "PV 22222"));

        await environment.Context.SaveChangesAsync();

        var request =
            new UpdateEmployerProfileRequest(
                "Second Holdings",
                "A complete updated Employer company profile.",
                "Colombo",
                "Second Contact",
                "HR Manager",
                " pv   11111 ",
                "0777654321",
                "https://second.example.com");

        var service = CreateService(environment);

        var result = await service.UpdateOwnProfileAsync(
            secondUser.Id,
            request,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            EmployerProfileUpdateFailureReason
                .DuplicateBusinessRegistrationNumber,
            result.FailureReason);
    }

    [Fact]
    public async Task Approved_identity_change_resets_verification_to_pending()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            EmployerVerificationStatus.Approved);

        var profile = CreateProfile(user.Id);

        environment.Context.EmployerProfiles.Add(profile);
        await environment.Context.SaveChangesAsync();

        environment.Clock.UtcNow =
            new DateTime(
                2026,
                9,
                11,
                2,
                0,
                0,
                DateTimeKind.Utc);

        var originalCompanyName = profile.CompanyName;
        var originalNormalizedCompanyName =
            profile.NormalizedCompanyName;

        var request =
            new UpdateEmployerProfileRequest(
                "Different Company Name",
                profile.Description,
                profile.Location,
                profile.ContactPersonName,
                profile.ContactPersonDesignation,
                profile.BusinessRegistrationNumber,
                profile.MobileNumber,
                profile.CompanyWebsite);

        var service = CreateService(environment);

        var result = await service.UpdateOwnProfileAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            EmployerProfileUpdateFailureReason.VerificationResetRequired,
            result.FailureReason);

        environment.Context.ChangeTracker.Clear();

        var savedUser =
            await environment.Context.Users
                .SingleAsync(candidate => candidate.Id == user.Id);

        var savedProfile =
            await environment.Context.EmployerProfiles
                .SingleAsync(candidate => candidate.UserId == user.Id);

        Assert.Equal(
            EmployerVerificationStatus.Pending,
            savedUser.EmployerVerificationStatus);

        Assert.Equal(
            environment.Clock.UtcNow,
            savedUser.UpdatedAtUtc);

        Assert.Equal(
            originalCompanyName,
            savedProfile.CompanyName);

        Assert.Equal(
            originalNormalizedCompanyName,
            savedProfile.NormalizedCompanyName);
    }

    [Fact]
    public async Task Approved_cosmetic_identity_change_does_not_reset_verification()
    {
        using var environment = CreateEnvironment();

        var user = await CreateEmployerAsync(
            environment,
            EmployerVerificationStatus.Approved);

        var profile = CreateProfile(user.Id);

        environment.Context.EmployerProfiles.Add(profile);
        await environment.Context.SaveChangesAsync();

        var request =
            new UpdateEmployerProfileRequest(
                "  acme   holdings  ",
                "An updated and complete Employer company profile.",
                "Colombo",
                "Test Contact",
                "HR Manager",
                " pv   12345 ",
                "0771234567",
                "https://example.com");

        var service = CreateService(environment);

        var result = await service.UpdateOwnProfileAsync(
            user.Id,
            request,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Profile);

        environment.Context.ChangeTracker.Clear();

        var savedUser =
            await environment.Context.Users
                .SingleAsync(candidate => candidate.Id == user.Id);

        var savedProfile =
            await environment.Context.EmployerProfiles
                .SingleAsync(candidate => candidate.UserId == user.Id);

        Assert.Equal(
            EmployerVerificationStatus.Approved,
            savedUser.EmployerVerificationStatus);

        Assert.Equal(
            "ACME HOLDINGS",
            savedProfile.NormalizedCompanyName);

        Assert.Equal(
            "PV 12345",
            savedProfile.NormalizedBusinessRegistrationNumber);

        Assert.True(result.Profile!.IsVacancyReady);
    }

    private static EmployerProfileService CreateService(
        TestEnvironment environment)
    {
        return new EmployerProfileService(
            environment.Context,
            environment.UserManager,
            environment.Clock);
    }

    private static TestEnvironment CreateEnvironment()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<HireSyncDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"HireSyncEmployerProfileTests-{Guid.NewGuid()}"));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<HireSyncDbContext>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<HireSyncDbContext>();

        context.Database.EnsureCreated();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        return new TestEnvironment(
            provider,
            scope,
            context,
            userManager,
            roleManager,
            new FakeClock());
    }

    private static async Task<ApplicationUser> CreateEmployerAsync(
        TestEnvironment environment,
        EmployerVerificationStatus verificationStatus,
        string email = "employer@example.com")
    {
        if (!await environment.RoleManager.RoleExistsAsync(
                RoleNames.Employer))
        {
            var roleResult =
                await environment.RoleManager.CreateAsync(
                    new IdentityRole<Guid>(
                        RoleNames.Employer));

            Assert.True(
                roleResult.Succeeded,
                string.Join(
                    "; ",
                    roleResult.Errors.Select(error => error.Description)));
        }

        var now =
            new DateTime(
                2026,
                9,
                10,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = "Test Employer",
            AccountStatus = AccountStatus.Active,
            EmployerVerificationStatus = verificationStatus,
            TokenVersion = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var createResult =
            await environment.UserManager.CreateAsync(user);

        Assert.True(
            createResult.Succeeded,
            string.Join(
                "; ",
                createResult.Errors.Select(error => error.Description)));

        var roleAssignment =
            await environment.UserManager.AddToRoleAsync(
                user,
                RoleNames.Employer);

        Assert.True(
            roleAssignment.Succeeded,
            string.Join(
                "; ",
                roleAssignment.Errors.Select(error => error.Description)));

        return user;
    }

    private static EmployerProfile CreateProfile(
        Guid userId,
        string companyName = "Acme Holdings",
        string businessRegistrationNumber = "PV 12345")
    {
        var createdAtUtc =
            new DateTime(
                2026,
                9,
                10,
                12,
                30,
                0,
                DateTimeKind.Utc);

        return new EmployerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,

            CompanyName =
                CompanyNameNormalizer.CanonicalizeDisplayName(
                    companyName),

            NormalizedCompanyName =
                CompanyNameNormalizer.Normalize(companyName),

            Description =
                "A complete Employer company profile.",

            Location =
                LocationNormalizer.CanonicalizeDisplayLocation(
                    "Colombo"),

            NormalizedLocation =
                LocationNormalizer.Normalize("Colombo"),

            ContactPersonName = "Test Contact",
            ContactPersonDesignation = "HR Manager",

            BusinessRegistrationNumber =
                BusinessRegistrationNumberNormalizer
                    .CanonicalizeDisplayValue(
                        businessRegistrationNumber),

            NormalizedBusinessRegistrationNumber =
                BusinessRegistrationNumberNormalizer.Normalize(
                    businessRegistrationNumber),

            MobileNumber = "0771234567",
            CompanyWebsite = "https://example.com",

            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        };
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } =
            new(
                2026,
                9,
                10,
                13,
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