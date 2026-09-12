using HireSync.Api.Controllers;
using HireSync.Application.DTOs.EmployerApplications;
using HireSync.Application.Interfaces.EmployerApplications;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class
    EmployerVacancyApplicantsControllerContractTests
{
    [Fact]
    public void Controller_requires_Employer_role()
    {
        var authorize =
            typeof(
                EmployerVacancyApplicantsController)
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            RoleNames.Employer,
            authorize.Roles);
    }

    [Fact]
    public void Controller_uses_canonical_applicant_route()
    {
        var route =
            typeof(
                EmployerVacancyApplicantsController)
            .GetCustomAttributes(
                typeof(RouteAttribute),
                inherit: true)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal(
            "api/v1/employer/vacancies/{vacancyId:guid}/applicants",
            route.Template);

        var method =
            typeof(
                EmployerVacancyApplicantsController)
            .GetMethod(
                nameof(
                    EmployerVacancyApplicantsController
                        .GetApplicants));

        Assert.NotNull(method);

        var get =
            method!
                .GetCustomAttributes(
                    typeof(HttpGetAttribute),
                    inherit: true)
                .Cast<HttpGetAttribute>()
                .Single();

        Assert.Null(
            get.Template);
    }

    [Fact]
    public async Task Get_returns_200_and_forwards_request()
    {
        var employerUserId =
            Guid.NewGuid();

        var vacancyId =
            Guid.NewGuid();

        var request =
            new RankedApplicantListRequest(
                Page: 2,
                PageSize: 10);

        var expectedPage =
            new RankedApplicantPageDto(
                vacancyId,
                "Backend Developer",
                Array.Empty<RankedApplicantDto>(),
                2,
                10,
                0);

        var service =
            new FakeRankedApplicantService(
                RankedApplicantQueryResult
                    .Success(
                        expectedPage));

        var controller =
            new EmployerVacancyApplicantsController(
                service,
                new FakeCurrentUser(
                    employerUserId));

        var action =
            await controller.GetApplicants(
                vacancyId,
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        Assert.Same(
            expectedPage,
            ok.Value);

        Assert.Equal(
            employerUserId,
            service.LastEmployerUserId);

        Assert.Equal(
            vacancyId,
            service.LastVacancyId);

        Assert.Same(
            request,
            service.LastRequest);
    }

    [Fact]
    public async Task Get_returns_401_for_invalid_current_user()
    {
        var controller =
            new EmployerVacancyApplicantsController(
                new FakeRankedApplicantService(
                    RankedApplicantQueryResult
                        .Failure(
                            RankedApplicantQueryFailureReason
                                .InvalidInput)),
                new FakeCurrentUser(
                    null));

        var action =
            await controller.GetApplicants(
                Guid.NewGuid(),
                new RankedApplicantListRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);
    }

    [Theory]
    [InlineData(
        RankedApplicantQueryFailureReason.InvalidInput,
        StatusCodes.Status400BadRequest)]
    [InlineData(
        RankedApplicantQueryFailureReason.VacancyNotFound,
        StatusCodes.Status404NotFound)]
    [InlineData(
        RankedApplicantQueryFailureReason.InvalidMatchingData,
        StatusCodes.Status500InternalServerError)]
    public async Task Get_maps_failure_to_expected_status(
        RankedApplicantQueryFailureReason failureReason,
        int expectedStatus)
    {
        var controller =
            new EmployerVacancyApplicantsController(
                new FakeRankedApplicantService(
                    RankedApplicantQueryResult
                        .Failure(
                            failureReason)),
                new FakeCurrentUser(
                    Guid.NewGuid()));

        var action =
            await controller.GetApplicants(
                Guid.NewGuid(),
                new RankedApplicantListRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            expectedStatus,
            problem.StatusCode);
    }

    private sealed class FakeRankedApplicantService
        : IRankedApplicantService
    {
        private readonly RankedApplicantQueryResult
            _result;

        public FakeRankedApplicantService(
            RankedApplicantQueryResult result)
        {
            _result =
                result;
        }

        public Guid? LastEmployerUserId
        {
            get;
            private set;
        }

        public Guid? LastVacancyId
        {
            get;
            private set;
        }

        public RankedApplicantListRequest?
            LastRequest
        {
            get;
            private set;
        }

        public Task<RankedApplicantQueryResult>
            GetOwnVacancyApplicantsAsync(
                Guid employerUserId,
                Guid vacancyId,
                RankedApplicantListRequest request,
                CancellationToken cancellationToken = default)
        {
            LastEmployerUserId =
                employerUserId;

            LastVacancyId =
                vacancyId;

            LastRequest =
                request;

            return Task.FromResult(
                _result);
        }
    }

    private sealed class FakeCurrentUser
        : ICurrentUser
    {
        public FakeCurrentUser(
            Guid? userId)
        {
            UserId =
                userId;
        }

        public bool IsAuthenticated =>
            UserId.HasValue;

        public Guid? UserId
        {
            get;
        }

        public string? Role =>
            RoleNames.Employer;

        public string? Email =>
            "employer@example.com";
    }
}