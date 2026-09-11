using HireSync.Application.DTOs.Auth;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HireSync.Api.IntegrationTests;

public sealed class EmployerRegistrationProvisionerTests
{
    [Fact]
    public async Task ProvisionAsync_creates_employer_and_profile_atomically()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var service = CreateService(environment);

        var result = await service.ProvisionAsync(
            CreateValidRequest());

        Assert.True(result.Succeeded, $"Provisioning failed: {result.FailureReason}");
        Assert.NotNull(result.Response);

        var user =
            await environment.UserManager.FindByEmailAsync(
                "employer@example.com");

        Assert.NotNull(user);

        Assert.Equal(
            AccountStatus.Active,
            user.AccountStatus);

        Assert.Equal(
            EmployerVerificationStatus.Pending,
            user.EmployerVerificationStatus);

        Assert.False(user.EmailConfirmed);

        Assert.Equal(
            "Test Contact",
            user.DisplayName);

        Assert.True(
            await environment.UserManager.IsInRoleAsync(
                user,
                RoleNames.Employer));

        environment.Context.ChangeTracker.Clear();

        var profiles =
            await environment.Context.EmployerProfiles
                .ToListAsync();

        var profile = Assert.Single(profiles);

        Assert.Equal(user.Id, profile.UserId);

        Assert.Equal(
            "Example Holdings",
            profile.CompanyName);

        Assert.Equal(
            "EXAMPLE HOLDINGS",
            profile.NormalizedCompanyName);

        Assert.Equal(
            "Colombo Central",
            profile.Location);

        Assert.Equal(
            "COLOMBO CENTRAL",
            profile.NormalizedLocation);

        Assert.Equal(
            "pv 12345/a",
            profile.BusinessRegistrationNumber);

        Assert.Equal(
            "PV 12345/A",
            profile.NormalizedBusinessRegistrationNumber);

        Assert.Equal(
            "Test Contact",
            profile.ContactPersonName);

        Assert.Equal(
            "HR Manager",
            profile.ContactPersonDesignation);

        Assert.Equal(
            "0771234567",
            profile.MobileNumber);

        Assert.Equal(
            "https://example.com",
            profile.CompanyWebsite);

        Assert.Equal(
            environment.Clock.UtcNow,
            profile.CreatedAtUtc);

        Assert.Equal(
            environment.Clock.UtcNow,
            profile.UpdatedAtUtc);

        Assert.Equal(
            user.Id,
            result.Response!.UserId);

        Assert.Equal(
            profile.Id,
            result.Response.EmployerProfileId);

        Assert.Equal(
            RoleNames.Employer,
            result.Response.Role);

        Assert.Equal(
            EmployerVerificationStatus.Pending,
            result.Response.EmployerVerificationStatus);
    }

    [Fact]
    public async Task ProvisionAsync_rejects_duplicate_email()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var service = CreateService(environment);

        var first =
            await service.ProvisionAsync(
                CreateValidRequest());

        Assert.True(first.Succeeded);

        var duplicateRequest =
            CreateValidRequest() with
            {
                CompanyName = "Second Holdings",
                BusinessRegistrationNumber = "PV 98765",
            };

        var second =
            await service.ProvisionAsync(
                duplicateRequest);

        Assert.False(second.Succeeded);

        Assert.Equal(
            EmployerRegistrationFailureReason.EmailAlreadyExists,
            second.FailureReason);

        environment.Context.ChangeTracker.Clear();

        Assert.Equal(
            1,
            await environment.Context.Users.CountAsync());

        Assert.Equal(
            1,
            await environment.Context.EmployerProfiles.CountAsync());
    }

    [Fact]
    public async Task ProvisionAsync_rejects_duplicate_business_registration_number()
    {
        using var environment =
            await CreateEnvironmentAsync();

        var service = CreateService(environment);

        var first =
            await service.ProvisionAsync(
                CreateValidRequest());

        Assert.True(first.Succeeded);

        var duplicateRequest =
            CreateValidRequest() with
            {
                Email = "second-employer@example.com",
                CompanyName = "Second Holdings",
                BusinessRegistrationNumber = "  pv   12345/a  ",
            };

        var second =
            await service.ProvisionAsync(
                duplicateRequest);

        Assert.False(second.Succeeded);

        Assert.Equal(
            EmployerRegistrationFailureReason
                .DuplicateBusinessRegistrationNumber,
            second.FailureReason);

        environment.Context.ChangeTracker.Clear();

        Assert.False(
            await environment.Context.Users.AnyAsync(
                user =>
                    user.Email ==
                    "second-employer@example.com"));

        Assert.Equal(
            1,
            await environment.Context.EmployerProfiles.CountAsync());
    }

    [Fact]
    public async Task ProvisionAsync_rolls_back_identity_when_profile_persistence_fails()
    {
        using var environment =
            await CreateEnvironmentAsync(
                new FailEmployerProfileSaveInterceptor());

        var service = CreateService(environment);

        var request =
            CreateValidRequest() with
            {
                Email = "rollback@example.com",
            };

        var result =
            await service.ProvisionAsync(request);

        Assert.False(result.Succeeded);

        Assert.Equal(
            EmployerRegistrationFailureReason.PersistenceFailed,
            result.FailureReason);

        environment.Context.ChangeTracker.Clear();

        Assert.False(
            await environment.Context.Users.AnyAsync(
                user =>
                    user.Email == "rollback@example.com"));

        Assert.False(
            await environment.Context
                .Set<IdentityUserRole<Guid>>()
                .AnyAsync());

        Assert.Empty(
            await environment.Context.EmployerProfiles
                .ToListAsync());
    }

    private static EmployerRegistrationProvisioner CreateService(
        TestEnvironment environment)
    {
        var identityService =
            new IdentityService(
                environment.UserManager,
                environment.Clock);

        return new EmployerRegistrationProvisioner(
            environment.Context,
            identityService,
            environment.Clock);
    }

    private static RegisterEmployerRequest CreateValidRequest()
    {
        return new RegisterEmployerRequest(
            " employer@example.com ",
            "ValidPassword123!",
            "  Example   Holdings ",
            "A complete Employer organisation profile description.",
            "  Colombo   Central ",
            " Test Contact ",
            " HR Manager ",
            " pv   12345/a ",
            " 0771234567 ",
            " https://example.com ");
    }

    private static async Task<TestEnvironment>
        CreateEnvironmentAsync(
            IInterceptor? interceptor = null)
    {
        var server =
            Environment.GetEnvironmentVariable(
                "HIRESYNC_TEST_SQLSERVER");

        if (string.IsNullOrWhiteSpace(server))
        {
            server = @".\SQLEXPRESS";
        }

        var databaseName =
            $"HireSyncEmployerRegistrationTests_{Guid.NewGuid():N}";

        var connectionString =
            $"Server={server};" +
            $"Database={databaseName};" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;" +
            "MultipleActiveResultSets=True";

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<HireSyncDbContext>(
            options =>
            {
                options.UseSqlServer(connectionString);

                if (interceptor is not null)
                {
                    options.AddInterceptors(interceptor);
                }
            });

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

        await context.Database.EnsureCreatedAsync();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(
                RoleNames.Employer))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole<Guid>(
                        RoleNames.Employer));

            Assert.True(
                roleResult.Succeeded,
                string.Join(
                    "; ",
                    roleResult.Errors.Select(
                        error => error.Description)));
        }

        return new TestEnvironment(
            provider,
            scope,
            context,
            userManager,
            roleManager,
            new FakeClock());
    }

    private sealed class FailEmployerProfileSaveInterceptor
        : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>>
            SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
        {
            var hasAddedEmployerProfile =
                eventData.Context?
                    .ChangeTracker
                    .Entries<EmployerProfile>()
                    .Any(
                        entry =>
                            entry.State ==
                            EntityState.Added) == true;

            if (hasAddedEmployerProfile)
            {
                throw new DbUpdateException(
                    "Forced EmployerProfile persistence failure.");
            }

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; } =
            new(
                2026,
                9,
                12,
                1,
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
            try
            {
                Context.Database.EnsureDeleted();
            }
            finally
            {
                _scope.Dispose();
                _provider.Dispose();
            }
        }
    }
}