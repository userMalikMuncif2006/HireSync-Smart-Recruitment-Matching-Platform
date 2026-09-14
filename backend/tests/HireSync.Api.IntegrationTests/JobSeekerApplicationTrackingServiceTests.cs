using HireSync.Application.DTOs.Applications;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Domain.Enums;
using HireSync.Infrastructure.Applications;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Api.IntegrationTests;

public sealed class JobSeekerApplicationTrackingServiceTests
{
    private static readonly DateTime BaseUtc =
        new(
            2026,
            9,
            12,
            10,
            0,
            0,
            DateTimeKind.Utc);

    [Fact]
    public async Task Own_records_only_and_multiple_statuses_are_returned()
    {
        await using var context =
            await CreateContextAsync();

        var ownUser =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var otherUser =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var ownProfile =
            await AddJobSeekerProfileAsync(
                context,
                ownUser.Id);

        var otherProfile =
            await AddJobSeekerProfileAsync(
                context,
                otherUser.Id);

        var employer =
            await AddEmployerProfileAsync(
                context,
                "Northwind Labs");

        var ownApplied =
            await AddApplicationAsync(
                context,
                ownProfile.Id,
                employer.Id,
                "Backend Engineer",
                BaseUtc.AddMinutes(3));

        var ownReview =
            await AddApplicationAsync(
                context,
                ownProfile.Id,
                employer.Id,
                "Frontend Engineer",
                BaseUtc.AddMinutes(2),
                ApplicationStatus.UnderReview);

        var foreign =
            await AddApplicationAsync(
                context,
                otherProfile.Id,
                employer.Id,
                "Foreign Application",
                BaseUtc.AddMinutes(4));

        context.ChangeTracker.Clear();

        var service =
            new JobSeekerApplicationTrackingService(
                context);

        var result =
            await service.GetOwnApplicationsAsync(
                ownUser.Id,
                new JobSeekerApplicationListRequest());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Page);
        Assert.Equal(2, result.Page!.TotalCount);
        Assert.Equal(2, result.Page.Items.Count);

        Assert.Equal(
            ownApplied.Application.Id,
            result.Page.Items[0].ApplicationId);

        Assert.Equal(
            ApplicationStatus.Applied,
            result.Page.Items[0].Status);

        Assert.Equal(
            ownReview.Application.Id,
            result.Page.Items[1].ApplicationId);

        Assert.Equal(
            ApplicationStatus.UnderReview,
            result.Page.Items[1].Status);

        Assert.DoesNotContain(
            result.Page.Items,
            item =>
                item.ApplicationId ==
                foreign.Application.Id);

        Assert.All(
            result.Page.Items,
            item =>
                Assert.Equal(
                    "Northwind Labs",
                    item.CompanyName));
    }

    [Fact]
    public async Task Status_filter_returns_only_requested_status()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var profile =
            await AddJobSeekerProfileAsync(
                context,
                user.Id);

        var employer =
            await AddEmployerProfileAsync(
                context,
                "Contoso Recruitment");

        _ =
            await AddApplicationAsync(
                context,
                profile.Id,
                employer.Id,
                "Applied Vacancy",
                BaseUtc,
                ApplicationStatus.Applied);

        var review =
            await AddApplicationAsync(
                context,
                profile.Id,
                employer.Id,
                "Review Vacancy",
                BaseUtc.AddMinutes(1),
                ApplicationStatus.UnderReview);

        context.ChangeTracker.Clear();

        var service =
            new JobSeekerApplicationTrackingService(
                context);

        var result =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest(
                    ApplicationStatus.UnderReview));

        Assert.True(result.Succeeded);

        var item =
            Assert.Single(
                result.Page!.Items);

        Assert.Equal(
            review.Application.Id,
            item.ApplicationId);

        Assert.Equal(
            ApplicationStatus.UnderReview,
            item.Status);
    }

    [Fact]
    public async Task Pagination_and_ordering_are_deterministic()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var profile =
            await AddJobSeekerProfileAsync(
                context,
                user.Id);

        var employer =
            await AddEmployerProfileAsync(
                context,
                "Fabrikam");

        var secondId =
            Guid.Parse(
                "00000000-0000-0000-0000-000000000002");

        var firstId =
            Guid.Parse(
                "00000000-0000-0000-0000-000000000001");

        _ =
            await AddApplicationAsync(
                context,
                profile.Id,
                employer.Id,
                "Older Vacancy",
                BaseUtc,
                applicationId:
                    Guid.Parse(
                        "00000000-0000-0000-0000-000000000003"));

        _ =
            await AddApplicationAsync(
                context,
                profile.Id,
                employer.Id,
                "Same Time Second Id",
                BaseUtc.AddMinutes(5),
                applicationId:
                    secondId);

        _ =
            await AddApplicationAsync(
                context,
                profile.Id,
                employer.Id,
                "Same Time First Id",
                BaseUtc.AddMinutes(5),
                applicationId:
                    firstId);

        context.ChangeTracker.Clear();

        var service =
            new JobSeekerApplicationTrackingService(
                context);

        var firstPage =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest(
                    Page: 1,
                    PageSize: 2));

        Assert.True(firstPage.Succeeded);
        Assert.Equal(3, firstPage.Page!.TotalCount);
        Assert.Equal(2, firstPage.Page.Items.Count);
        Assert.Equal(
            firstId,
            firstPage.Page.Items[0].ApplicationId);
        Assert.Equal(
            secondId,
            firstPage.Page.Items[1].ApplicationId);

        var secondPage =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest(
                    Page: 2,
                    PageSize: 2));

        Assert.True(secondPage.Succeeded);

        var remaining =
            Assert.Single(
                secondPage.Page!.Items);

        Assert.Equal(
            "Older Vacancy",
            remaining.VacancyTitle);
    }

    [Fact]
    public async Task Empty_result_is_successful()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        _ =
            await AddJobSeekerProfileAsync(
                context,
                user.Id);

        context.ChangeTracker.Clear();

        var service =
            new JobSeekerApplicationTrackingService(
                context);

        var result =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Page);
        Assert.Empty(result.Page!.Items);
        Assert.Equal(0, result.Page.TotalCount);
    }

    [Fact]
    public async Task Closed_vacancy_application_remains_visible()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var profile =
            await AddJobSeekerProfileAsync(
                context,
                user.Id);

        var employer =
            await AddEmployerProfileAsync(
                context,
                "Adventure Works");

        var created =
            await AddApplicationAsync(
                context,
                profile.Id,
                employer.Id,
                "Historical Vacancy",
                BaseUtc);

        created.Vacancy.Close(
            BaseUtc.AddHours(1));

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var service =
            new JobSeekerApplicationTrackingService(
                context);

        var result =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest());

        Assert.True(result.Succeeded);

        var item =
            Assert.Single(
                result.Page!.Items);

        Assert.Equal(
            VacancyStatus.Closed,
            item.VacancyStatus);

        Assert.Equal(
            "Historical Vacancy",
            item.VacancyTitle);
    }

    [Fact]
    public async Task Invalid_status_and_page_are_rejected()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Active);

        var service =
            new JobSeekerApplicationTrackingService(
                context);

        var invalidPage =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest(
                    Page: 0));

        Assert.False(
            invalidPage.Succeeded);

        Assert.Equal(
            JobSeekerApplicationTrackingFailureReason.InvalidInput,
            invalidPage.FailureReason);

        var invalidStatus =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest(
                    (ApplicationStatus)255));

        Assert.False(
            invalidStatus.Succeeded);

        Assert.Equal(
            JobSeekerApplicationTrackingFailureReason.InvalidInput,
            invalidStatus.FailureReason);
    }

    [Fact]
    public async Task Wrong_role_is_rejected()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.Employer,
                AccountStatus.Active);

        context.ChangeTracker.Clear();

        var service =
            new JobSeekerApplicationTrackingService(
                context);

        var result =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            JobSeekerApplicationTrackingFailureReason
                .JobSeekerUnavailable,
            result.FailureReason);
    }

    [Fact]
    public async Task Inactive_job_seeker_is_rejected()
    {
        await using var context =
            await CreateContextAsync();

        var user =
            await AddUserWithRoleAsync(
                context,
                RoleNames.JobSeeker,
                AccountStatus.Suspended);

        context.ChangeTracker.Clear();

        var service =
            new JobSeekerApplicationTrackingService(
                context);

        var result =
            await service.GetOwnApplicationsAsync(
                user.Id,
                new JobSeekerApplicationListRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            JobSeekerApplicationTrackingFailureReason
                .JobSeekerUnavailable,
            result.FailureReason);
    }

    private static async Task<HireSyncDbContext>
        CreateContextAsync()
    {
        var options =
            new DbContextOptionsBuilder<
                    HireSyncDbContext>()
                .UseInMemoryDatabase(
                    $"HireSync-ApplicationTracking-{Guid.NewGuid():N}")
                .Options;

        var context =
            new HireSyncDbContext(
                options);

        await context.Database
            .EnsureCreatedAsync();

        return context;
    }

    private static async Task<ApplicationUser>
        AddUserWithRoleAsync(
            HireSyncDbContext context,
            string roleName,
            AccountStatus accountStatus)
    {
        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName =
                    $"{Guid.NewGuid():N}@hiresync.test",
                NormalizedUserName =
                    $"{Guid.NewGuid():N}@HIRESYNC.TEST",
                Email =
                    $"{Guid.NewGuid():N}@hiresync.test",
                NormalizedEmail =
                    $"{Guid.NewGuid():N}@HIRESYNC.TEST",
                DisplayName =
                    "Test User",
                AccountStatus =
                    accountStatus,
                CreatedAtUtc =
                    BaseUtc,
                UpdatedAtUtc =
                    BaseUtc
            };

        var role =
            await context.Roles
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Name == roleName);

        if (role is null)
        {
            role =
                new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName =
                        roleName.ToUpperInvariant()
                };

            context.Roles.Add(role);
        }

        context.Users.Add(user);

        context.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = role.Id
            });

        await context.SaveChangesAsync();

        return user;
    }

    private static async Task<JobSeekerProfile>
        AddJobSeekerProfileAsync(
            HireSyncDbContext context,
            Guid userId)
    {
        var profile =
            new JobSeekerProfile(
                Guid.NewGuid(),
                userId,
                BaseUtc);

        context.JobSeekerProfiles.Add(
            profile);

        await context.SaveChangesAsync();

        return profile;
    }

    private static async Task<EmployerProfile>
        AddEmployerProfileAsync(
            HireSyncDbContext context,
            string companyName)
    {
        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName =
                    $"{Guid.NewGuid():N}@employer.test",
                NormalizedUserName =
                    $"{Guid.NewGuid():N}@EMPLOYER.TEST",
                Email =
                    $"{Guid.NewGuid():N}@employer.test",
                NormalizedEmail =
                    $"{Guid.NewGuid():N}@EMPLOYER.TEST",
                DisplayName =
                    companyName,
                AccountStatus =
                    AccountStatus.Active,
                EmployerVerificationStatus =
                    EmployerVerificationStatus.Approved,
                CreatedAtUtc =
                    BaseUtc,
                UpdatedAtUtc =
                    BaseUtc
            };

        var profile =
            new EmployerProfile
            {
                Id =
                    Guid.NewGuid(),
                UserId =
                    user.Id,
                CompanyName =
                    companyName,
                NormalizedCompanyName =
                    companyName.ToUpperInvariant(),
                Description =
                    "A verified employer profile used for application tracking tests.",
                Location =
                    "Colombo",
                NormalizedLocation =
                    "COLOMBO",
                ContactPersonName =
                    "Employer Contact",
                ContactPersonDesignation =
                    "Recruiter",
                BusinessRegistrationNumber =
                    Guid.NewGuid()
                        .ToString("N"),
                NormalizedBusinessRegistrationNumber =
                    Guid.NewGuid()
                        .ToString("N")
                        .ToUpperInvariant(),
                MobileNumber =
                    "0771234567",
                CreatedAtUtc =
                    BaseUtc,
                UpdatedAtUtc =
                    BaseUtc
            };

        context.Users.Add(user);
        context.EmployerProfiles.Add(
            profile);

        await context.SaveChangesAsync();

        return profile;
    }

    private static async Task<
        (JobApplication Application, Vacancy Vacancy)>
        AddApplicationAsync(
            HireSyncDbContext context,
            Guid jobSeekerProfileId,
            Guid employerProfileId,
            string vacancyTitle,
            DateTime appliedAtUtc,
            ApplicationStatus status =
                ApplicationStatus.Applied,
            Guid? applicationId = null)
    {
        var vacancy =
            new Vacancy(
                Guid.NewGuid(),
                employerProfileId,
                vacancyTitle,
                "This vacancy description is long enough for the canonical validation rules.",
                "Colombo",
                0,
                null,
                appliedAtUtc.AddDays(-1));

        var application =
            new JobApplication(
                applicationId ??
                    Guid.NewGuid(),
                vacancy.Id,
                jobSeekerProfileId,
                appliedAtUtc);

        if (status !=
            ApplicationStatus.Applied)
        {
            application.ChangeStatus(
                status,
                appliedAtUtc.AddMinutes(1));
        }

        context.Vacancies.Add(
            vacancy);

        context.JobApplications.Add(
            application);

        await context.SaveChangesAsync();

        return (
            application,
            vacancy);
    }
}
