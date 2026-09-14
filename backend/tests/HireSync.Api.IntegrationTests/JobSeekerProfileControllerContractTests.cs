using HireSync.Api.Controllers;
using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.JobSeeker;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class JobSeekerProfileControllerContractTests
{
    [Fact]
    public void Controller_requires_JobSeeker_role()
    {
        var attribute =
            typeof(JobSeekerProfileController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit: true)
                .Cast<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            RoleNames.JobSeeker,
            attribute.Roles);
    }

    [Fact]
    public void Controller_uses_seeker_profile_route()
    {
        var attribute =
            typeof(JobSeekerProfileController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit: true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/job-seeker/profile",
            attribute.Template);
    }

    [Fact]
    public async Task GetOwnProfile_returns_200_for_existing_profile()
    {
        var profile =
            CreateProfile();

        var service =
            new FakeJobSeekerProfileService
            {
                Profile = profile
            };

        var controller =
            new JobSeekerProfileController(
                service);

        var result =
            await controller.GetOwnProfile(
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<JobSeekerProfileDto>(
                ok.Value);

        Assert.Equal(
            200,
            ok.StatusCode);

        Assert.Equal(
            profile.TotalExperienceMonths,
            response.TotalExperienceMonths);

        Assert.Equal(
            profile.EducationLevel,
            response.EducationLevel);

        Assert.Equal(
            profile.PreferredLocation,
            response.PreferredLocation);

        Assert.True(
            response.IsMatchReady);

        Assert.Single(
            response.Skills);

        Assert.Equal(
            1,
            service.GetCallCount);
    }

    [Fact]
    public async Task GetOwnProfile_returns_404_when_profile_missing()
    {
        var service =
            new FakeJobSeekerProfileService
            {
                Profile = null
            };

        var controller =
            new JobSeekerProfileController(
                service);

        var result =
            await controller.GetOwnProfile(
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            404,
            problem.StatusCode);

        Assert.Equal(
            1,
            service.GetCallCount);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_200_for_success()
    {
        var profile =
            CreateProfile();

        var request =
            CreateUpdateRequest();

        var service =
            new FakeJobSeekerProfileService
            {
                UpdateResult =
                    JobSeekerProfileUpdateResult
                        .Success(profile)
            };

        var controller =
            new JobSeekerProfileController(
                service);

        var result =
            await controller.UpdateOwnProfile(
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<JobSeekerProfileDto>(
                ok.Value);

        Assert.Equal(
            200,
            ok.StatusCode);

        Assert.True(
            response.IsMatchReady);

        Assert.Same(
            request,
            service.LastUpdateRequest);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_401_for_invalid_authenticated_user()
    {
        var service =
            CreateFailureService(
                JobSeekerProfileUpdateFailureReason
                    .InvalidAuthenticatedUser);

        var controller =
            new JobSeekerProfileController(
                service);

        var result =
            await controller.UpdateOwnProfile(
                CreateUpdateRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            401,
            problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_400_for_invalid_input()
    {
        var service =
            CreateFailureService(
                JobSeekerProfileUpdateFailureReason
                    .InvalidInput);

        var controller =
            new JobSeekerProfileController(
                service);

        var result =
            await controller.UpdateOwnProfile(
                CreateUpdateRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            400,
            problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_400_for_unknown_skill()
    {
        var service =
            CreateFailureService(
                JobSeekerProfileUpdateFailureReason
                    .SkillNotFound);

        var controller =
            new JobSeekerProfileController(
                service);

        var result =
            await controller.UpdateOwnProfile(
                CreateUpdateRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            400,
            problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_returns_500_for_persistence_failure()
    {
        var service =
            CreateFailureService(
                JobSeekerProfileUpdateFailureReason
                    .PersistenceFailed);

        var controller =
            new JobSeekerProfileController(
                service);

        var result =
            await controller.UpdateOwnProfile(
                CreateUpdateRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            500,
            problem.StatusCode);
    }

    private static FakeJobSeekerProfileService
        CreateFailureService(
            JobSeekerProfileUpdateFailureReason failureReason)
    {
        return new FakeJobSeekerProfileService
        {
            UpdateResult =
                JobSeekerProfileUpdateResult
                    .Failure(
                        failureReason)
        };
    }

    private static JobSeekerProfileDto CreateProfile()
    {
        return new JobSeekerProfileDto(
            24,
            EducationLevel.Bachelor,
            "Colombo",
            new[]
            {
                new SkillSummaryDto(
                    Guid.NewGuid(),
                    "C#")
            },
            true);
    }

    private static UpdateJobSeekerProfileRequest
        CreateUpdateRequest()
    {
        return new UpdateJobSeekerProfileRequest(
            24,
            EducationLevel.Bachelor,
            "Colombo",
            new[]
            {
                Guid.NewGuid()
            });
    }

    private sealed class FakeJobSeekerProfileService
        : IJobSeekerProfileService
    {
        public JobSeekerProfileDto?
            Profile
        {
            get;
            init;
        }

        public JobSeekerProfileUpdateResult
            UpdateResult
        {
            get;
            init;
        } =
            JobSeekerProfileUpdateResult
                .Failure(
                    JobSeekerProfileUpdateFailureReason
                        .PersistenceFailed);

        public int GetCallCount
        {
            get;
            private set;
        }

        public UpdateJobSeekerProfileRequest?
            LastUpdateRequest
        {
            get;
            private set;
        }

        public Task<JobSeekerProfileDto?>
            GetOwnProfileAsync(
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            GetCallCount++;

            return Task.FromResult(
                Profile);
        }

        public Task<JobSeekerProfileUpdateResult>
            UpdateOwnProfileAsync(
                UpdateJobSeekerProfileRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            LastUpdateRequest =
                request;

            return Task.FromResult(
                UpdateResult);
        }
    }
}
