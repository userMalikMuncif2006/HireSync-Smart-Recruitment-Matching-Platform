using HireSync.Api.Controllers;
using HireSync.Application.DTOs;
using HireSync.Application.DTOs.Matching;
using HireSync.Application.Interfaces.Matching;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class JobMatchesControllerContractTests
{
    [Fact]
    public void Controller_requires_JobSeeker_role()
    {
        var authorize =
            typeof(JobMatchesController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit: true)
                .Cast<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            RoleNames.JobSeeker,
            authorize.Roles);
    }

    [Fact]
    public void Controller_uses_canonical_vacancy_route()
    {
        var route =
            typeof(JobMatchesController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit: true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/vacancies",
            route.Template);

        var method =
            typeof(JobMatchesController)
                .GetMethod(
                    nameof(JobMatchesController.GetMatch));

        Assert.NotNull(method);

        var httpGet =
            method!
                .GetCustomAttributes(
                    typeof(HttpGetAttribute),
                    inherit: true)
                .Cast<HttpGetAttribute>()
                .Single();

        Assert.Equal(
            "{vacancyId:guid}/match",
            httpGet.Template);
    }

    [Fact]
    public async Task GetMatch_returns_200_for_success()
    {
        var vacancyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var expected =
            new MatchResultDto(
                82.50m,
                37.50m,
                25.00m,
                10.00m,
                10.00m,
                new[]
                {
                    new SkillSummaryDto(
                        Guid.NewGuid(),
                        "C#")
                },
                new[]
                {
                    new SkillSummaryDto(
                        Guid.NewGuid(),
                        "SQL")
                });

        var service =
            new FakeJobMatchService(
                JobMatchQueryResult.Success(
                    expected));

        var controller =
            new JobMatchesController(
                service,
                new FakeCurrentUser(
                    true,
                    userId));

        var action =
            await controller.GetMatch(
                vacancyId,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        Assert.Same(
            expected,
            ok.Value);

        Assert.Equal(
            userId,
            service.LastUserId);

        Assert.Equal(
            vacancyId,
            service.LastVacancyId);
    }

    [Fact]
    public async Task GetMatch_returns_401_for_invalid_current_user()
    {
        var controller =
            new JobMatchesController(
                new FakeJobMatchService(
                    JobMatchQueryResult.Failure(
                        JobMatchFailureReason.InvalidInput)),
                new FakeCurrentUser(
                    false,
                    null));

        var action =
            await controller.GetMatch(
                Guid.NewGuid(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);
    }

    [Fact]
    public async Task GetMatch_returns_409_when_job_seeker_not_ready()
    {
        var controller =
            CreateController(
                JobMatchFailureReason.JobSeekerNotReady);

        var action =
            await controller.GetMatch(
                Guid.NewGuid(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            problem.StatusCode);
    }

    [Fact]
    public async Task GetMatch_returns_404_when_vacancy_unavailable()
    {
        var controller =
            CreateController(
                JobMatchFailureReason.VacancyUnavailable);

        var action =
            await controller.GetMatch(
                Guid.NewGuid(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            problem.StatusCode);
    }

    [Fact]
    public async Task GetMatch_returns_400_for_invalid_input()
    {
        var controller =
            CreateController(
                JobMatchFailureReason.InvalidInput);

        var action =
            await controller.GetMatch(
                Guid.Empty,
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem.StatusCode);
    }

    private static JobMatchesController CreateController(
        JobMatchFailureReason failureReason)
    {
        return new JobMatchesController(
            new FakeJobMatchService(
                JobMatchQueryResult.Failure(
                    failureReason)),
            new FakeCurrentUser(
                true,
                Guid.NewGuid()));
    }

    private sealed class FakeJobMatchService
        : IJobMatchService
    {
        private readonly JobMatchQueryResult _result;

        public FakeJobMatchService(
            JobMatchQueryResult result)
        {
            _result = result;
        }

        public Guid? LastUserId { get; private set; }

        public Guid? LastVacancyId { get; private set; }

        public Task<JobMatchQueryResult> GetMatchAsync(
            Guid jobSeekerUserId,
            Guid vacancyId,
            CancellationToken cancellationToken = default)
        {
            LastUserId = jobSeekerUserId;
            LastVacancyId = vacancyId;

            return Task.FromResult(_result);
        }
    }

    private sealed class FakeCurrentUser
        : ICurrentUser
    {
        public FakeCurrentUser(
            bool isAuthenticated,
            Guid? userId)
        {
            IsAuthenticated = isAuthenticated;
            UserId = userId;
        }

        public bool IsAuthenticated { get; }

        public Guid? UserId { get; }

        public string? Role =>
            RoleNames.JobSeeker;

        public string? Email =>
            "jobseeker@example.com";
    }
}
