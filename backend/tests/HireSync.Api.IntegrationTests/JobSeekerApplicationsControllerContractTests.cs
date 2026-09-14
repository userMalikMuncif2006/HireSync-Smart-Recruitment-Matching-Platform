using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Applications;
using HireSync.Application.Interfaces.Applications;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class JobSeekerApplicationsControllerContractTests
{
    [Fact]
    public void Controller_uses_canonical_route_and_JobSeeker_role()
    {
        var route =
            typeof(JobSeekerApplicationsController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit: true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/vacancies/{vacancyId:guid}/applications",
            route.Template);

        var authorize =
            typeof(JobSeekerApplicationsController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit: true)
                .Cast<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            RoleNames.JobSeeker,
            authorize.Roles);

        var method =
            typeof(JobSeekerApplicationsController)
                .GetMethod(
                    nameof(
                        JobSeekerApplicationsController
                            .CreateApplication));

        Assert.NotNull(method);

        Assert.Single(
            method!.GetCustomAttributes(
                typeof(HttpPostAttribute),
                inherit: true));
    }

    [Fact]
    public async Task Successful_apply_returns_201_and_created_application()
    {
        var vacancyId =
            Guid.NewGuid();

        var userId =
            Guid.NewGuid();

        var expected =
            new ApplicationCreatedDto(
                Guid.NewGuid(),
                vacancyId,
                ApplicationStatus.Applied,
                DateTime.UtcNow,
                DateTime.UtcNow);

        var service =
            new FakeJobApplicationService(
                ApplicationCreateResult.Success(
                    expected));

        var controller =
            CreateController(
                service,
                new FakeCurrentUser(
                    true,
                    userId));

        var action =
            await controller.CreateApplication(
                vacancyId,
                CancellationToken.None);

        var created =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            created.StatusCode);

        Assert.Same(
            expected,
            created.Value);

        Assert.Equal(
            userId,
            service.LastUserId);

        Assert.Equal(
            vacancyId,
            service.LastVacancyId);
    }

    [Fact]
    public async Task Invalid_current_user_returns_401_without_calling_service()
    {
        var service =
            new FakeJobApplicationService(
                ApplicationCreateResult.Failure(
                    ApplicationCreateFailureReason
                        .InvalidInput));

        var controller =
            CreateController(
                service,
                new FakeCurrentUser(
                    false,
                    null));

        var action =
            await controller.CreateApplication(
                Guid.NewGuid(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);

        Assert.Null(
            service.LastUserId);
    }

    [Theory]
    [InlineData(
        ApplicationCreateFailureReason.InvalidInput,
        StatusCodes.Status400BadRequest,
        "INVALID_APPLICATION_REQUEST")]
    [InlineData(
        ApplicationCreateFailureReason.JobSeekerUnavailable,
        StatusCodes.Status403Forbidden,
        "JOB_SEEKER_UNAVAILABLE")]
    [InlineData(
        ApplicationCreateFailureReason.ProfileNotReady,
        StatusCodes.Status400BadRequest,
        "PROFILE_NOT_READY")]
    [InlineData(
        ApplicationCreateFailureReason.CurrentCvRequired,
        StatusCodes.Status400BadRequest,
        "CURRENT_CV_REQUIRED")]
    [InlineData(
        ApplicationCreateFailureReason.VacancyUnavailable,
        StatusCodes.Status404NotFound,
        "VACANCY_UNAVAILABLE")]
    [InlineData(
        ApplicationCreateFailureReason.VacancyClosed,
        StatusCodes.Status409Conflict,
        "VACANCY_CLOSED")]
    [InlineData(
        ApplicationCreateFailureReason.AlreadyApplied,
        StatusCodes.Status409Conflict,
        "DUPLICATE_APPLICATION")]
    [InlineData(
        ApplicationCreateFailureReason.PersistenceFailed,
        StatusCodes.Status500InternalServerError,
        "APPLICATION_PERSISTENCE_FAILED")]
    public async Task Failure_reason_maps_to_expected_problem_contract(
        ApplicationCreateFailureReason reason,
        int expectedStatus,
        string expectedCode)
    {
        var controller =
            CreateController(
                new FakeJobApplicationService(
                    ApplicationCreateResult.Failure(
                        reason)),
                new FakeCurrentUser(
                    true,
                    Guid.NewGuid()));

        var action =
            await controller.CreateApplication(
                Guid.NewGuid(),
                CancellationToken.None);

        var result =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            expectedStatus,
            result.StatusCode);

        var problem =
            Assert.IsType<ProblemDetails>(
                result.Value);

        Assert.Equal(
            expectedStatus,
            problem.Status);

        Assert.True(
            problem.Extensions.TryGetValue(
                "code",
                out var code));

        Assert.Equal(
            expectedCode,
            code?.ToString());
    }
    private static JobSeekerApplicationsController
        CreateController(
            IJobApplicationService service,
            ICurrentUser currentUser)
    {
        return new JobSeekerApplicationsController(
            service,
            currentUser);
    }

    private sealed class FakeJobApplicationService
        : IJobApplicationService
    {
        private readonly ApplicationCreateResult
            _result;

        public FakeJobApplicationService(
            ApplicationCreateResult result)
        {
            _result =
                result;
        }

        public Guid? LastUserId { get; private set; }

        public Guid? LastVacancyId { get; private set; }

        public Task<ApplicationCreateResult>
            CreateAsync(
                Guid jobSeekerUserId,
                Guid vacancyId,
                CancellationToken cancellationToken = default)
        {
            LastUserId =
                jobSeekerUserId;

            LastVacancyId =
                vacancyId;

            return Task.FromResult(
                _result);
        }
    }

    private sealed class FakeCurrentUser
        : ICurrentUser
    {
        public FakeCurrentUser(
            bool isAuthenticated,
            Guid? userId)
        {
            IsAuthenticated =
                isAuthenticated;

            UserId =
                userId;
        }

        public bool IsAuthenticated { get; }

        public Guid? UserId { get; }

        public string? Role =>
            RoleNames.JobSeeker;

        public string? Email =>
            "jobseeker@example.com";
    }
}