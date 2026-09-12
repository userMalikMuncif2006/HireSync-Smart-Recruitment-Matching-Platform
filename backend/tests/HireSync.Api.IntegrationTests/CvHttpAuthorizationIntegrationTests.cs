using System.Net;
using System.Net.Http.Headers;
using HireSync.Api.Controllers;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HireSync.Api.IntegrationTests;

[CollectionDefinition(
    CvHttpAuthorizationCollection.Name,
    DisableParallelization = true)]
public sealed class CvHttpAuthorizationCollection
{
    public const string Name =
        "CV HTTP authorization non-parallel";
}

[Collection(
    CvHttpAuthorizationCollection.Name)]
public sealed class CvHttpAuthorizationIntegrationTests
    : IAsyncLifetime
{
    private const string CvMetadataPath =
        "/api/v1/job-seeker/cv";

    private const string CvFilePath =
        "/api/v1/job-seeker/cv/file";

    private const string ActiveJobSeekerEmail =
        "cv-active-jobseeker@example.test";

    private const string SuspendedJobSeekerEmail =
        "cv-suspended-jobseeker@example.test";

    private const string EmployerEmail =
        "cv-employer@example.test";

    private const string AdministratorEmail =
        "cv-administrator@example.test";

    private static readonly string JwtSigningKey =
        Convert.ToBase64String(
            Enumerable.Range(
                    1,
                    32)
                .Select(
                    value =>
                        (byte)value)
                .ToArray());

    private readonly Guid _activeJobSeekerId =
        Guid.NewGuid();

    private readonly Guid _suspendedJobSeekerId =
        Guid.NewGuid();

    private readonly Guid _employerId =
        Guid.NewGuid();

    private readonly Guid _administratorId =
        Guid.NewGuid();

    private readonly string _databaseName =
        $"HireSync-Cv-Http-Auth-{Guid.NewGuid():N}";

    private readonly string _storageRootPath =
        Path.Combine(
            Path.GetTempPath(),
            "HireSync-Cv-Http-Auth",
            Guid.NewGuid()
                .ToString("N"));

    private readonly Dictionary<string, string?>
        _previousEnvironment =
            new(
                StringComparer.Ordinal);

    private CvAuthorizationWebApplicationFactory?
        _factory;

    private HttpClient?
        _client;

    private bool _environmentApplied;

    public async Task InitializeAsync()
    {
        ApplyRequiredEnvironment();

        try
        {
            _factory =
                new CvAuthorizationWebApplicationFactory(
                    _databaseName);

            _client =
                _factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect =
                            false,

                        BaseAddress =
                            new Uri(
                                "https://localhost")
                    });

            await SeedIdentityStateAsync();
        }
        catch
        {
            await CleanupAsync();

            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await CleanupAsync();
    }

    [Fact]
    public async Task Anonymous_is_denied_from_all_cv_endpoints()
    {
        await AssertCvEndpointStatusesAsync(
            token:
                null,
            expectedMetadata:
                HttpStatusCode.Unauthorized,
            expectedUpload:
                HttpStatusCode.Unauthorized,
            expectedDownload:
                HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Active_Employer_is_forbidden_from_all_cv_endpoints()
    {
        var token =
            CreateToken(
                _employerId,
                EmployerEmail,
                RoleNames.Employer);

        await AssertCvEndpointStatusesAsync(
            token,
            expectedMetadata:
                HttpStatusCode.Forbidden,
            expectedUpload:
                HttpStatusCode.Forbidden,
            expectedDownload:
                HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Active_Administrator_is_forbidden_from_all_cv_endpoints()
    {
        var token =
            CreateToken(
                _administratorId,
                AdministratorEmail,
                RoleNames.Administrator);

        await AssertCvEndpointStatusesAsync(
            token,
            expectedMetadata:
                HttpStatusCode.Forbidden,
            expectedUpload:
                HttpStatusCode.Forbidden,
            expectedDownload:
                HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Suspended_JobSeeker_token_is_rejected_by_C1_on_all_cv_endpoints()
    {
        var token =
            CreateToken(
                _suspendedJobSeekerId,
                SuspendedJobSeekerEmail,
                RoleNames.JobSeeker);

        await AssertCvEndpointStatusesAsync(
            token,
            expectedMetadata:
                HttpStatusCode.Unauthorized,
            expectedUpload:
                HttpStatusCode.Unauthorized,
            expectedDownload:
                HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Active_JobSeeker_passes_authorization_on_all_cv_endpoints()
    {
        var token =
            CreateToken(
                _activeJobSeekerId,
                ActiveJobSeekerEmail,
                RoleNames.JobSeeker);

        await AssertCvEndpointStatusesAsync(
            token,
            expectedMetadata:
                HttpStatusCode.NotFound,
            expectedUpload:
                HttpStatusCode.NotFound,
            expectedDownload:
                HttpStatusCode.NotFound);
    }

    private async Task AssertCvEndpointStatusesAsync(
        string? token,
        HttpStatusCode expectedMetadata,
        HttpStatusCode expectedUpload,
        HttpStatusCode expectedDownload)
    {
        var metadataStatus =
            await SendCvRequestAsync(
                HttpMethod.Get,
                CvMetadataPath,
                token);

        Assert.Equal(
            expectedMetadata,
            metadataStatus);

        var uploadStatus =
            await SendCvRequestAsync(
                HttpMethod.Post,
                CvMetadataPath,
                token,
                includeValidCvUpload:
                    true);

        Assert.Equal(
            expectedUpload,
            uploadStatus);

        var downloadStatus =
            await SendCvRequestAsync(
                HttpMethod.Get,
                CvFilePath,
                token);

        Assert.Equal(
            expectedDownload,
            downloadStatus);
    }

    private async Task<HttpStatusCode>
        SendCvRequestAsync(
            HttpMethod method,
            string path,
            string? token,
            bool includeValidCvUpload =
                false)
    {
        if (_client is null)
        {
            throw new InvalidOperationException(
                "HTTP test client is not initialized.");
        }

        using var request =
            new HttpRequestMessage(
                method,
                path);

        if (!string.IsNullOrWhiteSpace(
                token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }

        if (includeValidCvUpload)
        {
            request.Content =
                CreateValidCvUploadContent();
        }

        using var response =
            await _client.SendAsync(
                request);

        return response.StatusCode;
    }

    private static MultipartFormDataContent
        CreateValidCvUploadContent()
    {
        var multipart =
            new MultipartFormDataContent(
                "HireSyncCvAuthorizationBoundary");

        var fileContent =
            new ByteArrayContent(
                "%PDF-1.7 synthetic authorization test"u8
                    .ToArray());

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "application/pdf");

        multipart.Add(
            fileContent,
            "file",
            "resume.pdf");

        return multipart;
    }

    private async Task SeedIdentityStateAsync()
    {
        if (_factory is null)
        {
            throw new InvalidOperationException(
                "HTTP test factory is not initialized.");
        }

        await using var scope =
            _factory.Services
                .CreateAsyncScope();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<
                        IdentityRole<Guid>>>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        await EnsureRoleAsync(
            roleManager,
            RoleNames.JobSeeker);

        await EnsureRoleAsync(
            roleManager,
            RoleNames.Employer);

        await EnsureRoleAsync(
            roleManager,
            RoleNames.Administrator);

        await CreateUserAsync(
            userManager,
            _activeJobSeekerId,
            ActiveJobSeekerEmail,
            RoleNames.JobSeeker,
            AccountStatus.Active);

        await CreateUserAsync(
            userManager,
            _suspendedJobSeekerId,
            SuspendedJobSeekerEmail,
            RoleNames.JobSeeker,
            AccountStatus.Suspended);

        await CreateUserAsync(
            userManager,
            _employerId,
            EmployerEmail,
            RoleNames.Employer,
            AccountStatus.Active);

        await CreateUserAsync(
            userManager,
            _administratorId,
            AdministratorEmail,
            RoleNames.Administrator,
            AccountStatus.Active);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole<Guid>>
            roleManager,
        string roleName)
    {
        if (await roleManager.RoleExistsAsync(
                roleName))
        {
            return;
        }

        var result =
            await roleManager.CreateAsync(
                new IdentityRole<Guid>
                {
                    Id =
                        Guid.NewGuid(),

                    Name =
                        roleName
                });

        Assert.True(
            result.Succeeded,
            FormatIdentityErrors(
                result));
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser>
            userManager,
        Guid userId,
        string email,
        string role,
        AccountStatus accountStatus)
    {
        var now =
            DateTime.UtcNow;

        var user =
            new ApplicationUser
            {
                Id =
                    userId,

                UserName =
                    email,

                Email =
                    email,

                EmailConfirmed =
                    true,

                DisplayName =
                    $"CV HTTP {role}",

                AccountStatus =
                    accountStatus,

                EmployerVerificationStatus =
                    null,

                TokenVersion =
                    1,

                CreatedAtUtc =
                    now,

                UpdatedAtUtc =
                    now
            };

        var createResult =
            await userManager.CreateAsync(
                user);

        Assert.True(
            createResult.Succeeded,
            FormatIdentityErrors(
                createResult));

        var roleResult =
            await userManager.AddToRoleAsync(
                user,
                role);

        Assert.True(
            roleResult.Succeeded,
            FormatIdentityErrors(
                roleResult));
    }

    private string CreateToken(
        Guid userId,
        string email,
        string role)
    {
        if (_factory is null)
        {
            throw new InvalidOperationException(
                "HTTP test factory is not initialized.");
        }

        using var scope =
            _factory.Services
                .CreateScope();

        var tokenService =
            scope.ServiceProvider
                .GetRequiredService<
                    ITokenService>();

        return tokenService
            .CreateAccessToken(
                userId,
                email,
                role,
                tokenVersion:
                    1)
            .Token;
    }

    private void ApplyRequiredEnvironment()
    {
        SetEnvironment(
            "ConnectionStrings__HireSyncDatabase",
            "Server=(localdb)\\MSSQLLocalDB;Database=HireSync_Cv_Http_Auth;Trusted_Connection=True;TrustServerCertificate=True;");

        SetEnvironment(
            "Storage__CvRoot",
            _storageRootPath);

        SetEnvironment(
            "Jwt__SigningKey",
            JwtSigningKey);

        SetEnvironment(
            "Jwt__Issuer",
            "HireSync.HttpAuthorization.Tests");

        SetEnvironment(
            "Jwt__Audience",
            "HireSync.HttpAuthorization.Tests");

        SetEnvironment(
            "Otp__HashingKey",
            "hire-sync-http-authorization-test-hashing-key");

        SetEnvironment(
            "Smtp__Host",
            "localhost");

        SetEnvironment(
            "Smtp__Port",
            "25");

        SetEnvironment(
            "Smtp__Username",
            "test-user");

        SetEnvironment(
            "Smtp__Password",
            "test-password");

        SetEnvironment(
            "Smtp__FromEmail",
            "no-reply@example.test");

        SetEnvironment(
            "Smtp__FromName",
            "HireSync Tests");

        SetEnvironment(
            "AdminSeed__Enabled",
            "false");

        _environmentApplied =
            true;
    }

    private void SetEnvironment(
        string key,
        string value)
    {
        if (!_previousEnvironment.ContainsKey(
                key))
        {
            _previousEnvironment[key] =
                Environment.GetEnvironmentVariable(
                    key);
        }

        Environment.SetEnvironmentVariable(
            key,
            value);
    }

    private void RestoreEnvironment()
    {
        if (!_environmentApplied)
        {
            return;
        }

        foreach (var previous in
            _previousEnvironment)
        {
            Environment.SetEnvironmentVariable(
                previous.Key,
                previous.Value);
        }

        _environmentApplied =
            false;
    }

    private async Task CleanupAsync()
    {
        _client?.Dispose();

        _client =
            null;

        if (_factory is not null)
        {
            await _factory.DisposeAsync();

            _factory =
                null;
        }

        RestoreEnvironment();

        if (Directory.Exists(
                _storageRootPath))
        {
            Directory.Delete(
                _storageRootPath,
                recursive:
                    true);
        }
    }

    private static string FormatIdentityErrors(
        IdentityResult result)
    {
        return string.Join(
            " | ",
            result.Errors.Select(
                error =>
                    $"{error.Code}: {error.Description}"));
    }

    private sealed class
        CvAuthorizationWebApplicationFactory
        : WebApplicationFactory<
            JobSeekerCvController>
    {
        private readonly string _databaseName;

        public CvAuthorizationWebApplicationFactory(
            string databaseName)
        {
            _databaseName =
                databaseName;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureServices(
                services =>
                {
                    services.RemoveAll<
                        DbContextOptions<
                            HireSyncDbContext>>();

                    services.RemoveAll<
                        HireSyncDbContext>();

                    services.AddScoped<
                        HireSyncDbContext>(
                        _ =>
                        {
                            var options =
                                new DbContextOptionsBuilder<
                                        HireSyncDbContext>()
                                    .UseInMemoryDatabase(
                                        _databaseName)
                                    .Options;

                            return new HireSyncDbContext(
                                options);
                        });
                });
        }
    }
}
