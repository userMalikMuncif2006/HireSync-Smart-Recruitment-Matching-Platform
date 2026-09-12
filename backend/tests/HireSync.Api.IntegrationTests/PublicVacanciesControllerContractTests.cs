using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Matching;
using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Matching;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Vacancy;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class PublicVacanciesControllerContractTests
{
    [Fact]
    public void Controller_requires_JobSeeker_role()
    {
        var authorize =
            typeof(PublicVacanciesController)
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
    public void Detail_uses_canonical_vacancy_route()
    {
        var route =
            typeof(PublicVacanciesController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit: true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/vacancies",
            route.Template);

        var method =
            typeof(PublicVacanciesController)
                .GetMethod(
                    nameof(
                        PublicVacanciesController
                            .GetVacancyDetail));

        Assert.NotNull(method);

        var httpGet =
            method!
                .GetCustomAttributes(
                    typeof(HttpGetAttribute),
                    inherit: true)
                .Cast<HttpGetAttribute>()
                .Single();

        Assert.Equal(
            "{vacancyId:guid}",
            httpGet.Template);
    }

    [Fact]
    public async Task Detail_returns_200_for_profile_incomplete_domain_state()
    {
        var vacancyId =
            Guid.NewGuid();

        var userId =
            Guid.NewGuid();

        var expected =
            new PublicVacancyDetailDto(
                vacancyId,
                "Backend Developer",
                "Build HireSync services.",
                "HireSync Employer",
                "Recruitment company",
                null,
                "Colombo",
                "Colombo",
                12,
                null,
                DateTime.UtcNow,
                Array.Empty<
                    HireSync.Application.DTOs.SkillSummaryDto>(),
                MatchStatusCodes.ProfileIncomplete,
                null,
                new[]
                {
                    "Skills"
                },
                false,
                false,
                DateTime.UtcNow);

        var detailService =
            new FakeVacancyDetailService(
                VacancyDetailQueryResult.Success(
                    expected));

        var controller =
            CreateController(
                detailService,
                new FakeCurrentUser(
                    true,
                    userId));

        var action =
            await controller.GetVacancyDetail(
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
            detailService.LastUserId);

        Assert.Equal(
            vacancyId,
            detailService.LastVacancyId);
    }

    [Fact]
    public async Task Detail_returns_401_for_invalid_current_user()
    {
        var detailService =
            new FakeVacancyDetailService(
                VacancyDetailQueryResult.Failure(
                    VacancyDetailFailureReason.InvalidInput));

        var controller =
            CreateController(
                detailService,
                new FakeCurrentUser(
                    false,
                    null));

        var action =
            await controller.GetVacancyDetail(
                Guid.NewGuid(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);

        Assert.Null(
            detailService.LastUserId);
    }

    [Fact]
    public async Task Detail_returns_404_for_unavailable_vacancy()
    {
        var controller =
            CreateController(
                new FakeVacancyDetailService(
                    VacancyDetailQueryResult.Failure(
                        VacancyDetailFailureReason
                            .VacancyUnavailable)),
                new FakeCurrentUser(
                    true,
                    Guid.NewGuid()));

        var action =
            await controller.GetVacancyDetail(
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
    public async Task Detail_returns_400_for_invalid_vacancy_id()
    {
        var controller =
            CreateController(
                new FakeVacancyDetailService(
                    VacancyDetailQueryResult.Failure(
                        VacancyDetailFailureReason
                            .InvalidInput)),
                new FakeCurrentUser(
                    true,
                    Guid.NewGuid()));

        var action =
            await controller.GetVacancyDetail(
                Guid.Empty,
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem.StatusCode);
    }

    private static PublicVacanciesController
        CreateController(
            IVacancyDetailService detailService,
            ICurrentUser currentUser)
    {
        return new PublicVacanciesController(
            new FakeVacancySearchService(),
            new NoOpVacancyMatchSearchService(),
            detailService,
            currentUser);
    }

    private sealed class FakeVacancySearchService
        : IVacancySearchService
    {
        public Task<PublicVacancyPageDto>
            SearchOpenVacanciesAsync(
                SearchVacanciesRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new PublicVacancyPageDto(
                    Array.Empty<
                        PublicVacancyListItemDto>(),
                    request.Page,
                    request.PageSize,
                    0));
        }
    }

    private sealed class FakeVacancyDetailService
        : IVacancyDetailService
    {
        private readonly VacancyDetailQueryResult _result;

        public FakeVacancyDetailService(
            VacancyDetailQueryResult result)
        {
            _result = result;
        }

        public Guid? LastUserId { get; private set; }

        public Guid? LastVacancyId { get; private set; }

        public Task<VacancyDetailQueryResult>
            GetDetailAsync(
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