using System.Security.Claims;
using HireSync.Api.Controllers;
using HireSync.Application.DTOs;
using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Employer;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class VacanciesControllerReadUpdateContractTests
{
    [Fact]
    public async Task GetOwnVacancies_returns_200_for_success()
    {
        var employerUserId = Guid.NewGuid();

        var page =
            new EmployerVacancyPageDto(
                new[]
                {
                    new EmployerVacancyListItemDto(
                        Guid.NewGuid(),
                        "Backend Developer",
                        "Colombo",
                        VacancyStatus.Open,
                        Utc(12),
                        Utc(12),
                        null,
                        RowVersion())
                },
                Page: 1,
                PageSize: 20,
                TotalCount: 1);

        var service =
            new FakeVacancyService
            {
                ListResult = page
            };

        var controller =
            CreateController(
                service,
                employerUserId);

        var request =
            new EmployerVacancyListRequest();

        var result =
            await controller.GetOwnVacancies(
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<EmployerVacancyPageDto>(
                ok.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        Assert.Equal(
            1,
            response.TotalCount);

        Assert.Equal(
            employerUserId,
            service.LastListEmployerUserId);

        Assert.Same(
            request,
            service.LastListRequest);
    }

    [Fact]
    public async Task GetOwnVacancies_returns_400_for_invalid_request()
    {
        var service =
            new FakeVacancyService();

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var request =
            new EmployerVacancyListRequest(
                Page: 0,
                PageSize: 20);

        var result =
            await controller.GetOwnVacancies(
                request,
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem.StatusCode);

        Assert.Null(
            service.LastListEmployerUserId);
    }

    [Fact]
    public async Task GetOwnVacancies_returns_404_when_context_missing()
    {
        var service =
            new FakeVacancyService
            {
                ListResult = null
            };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.GetOwnVacancies(
                new EmployerVacancyListRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            problem.StatusCode);
    }

    [Fact]
    public async Task GetOwnVacancies_returns_401_when_sub_claim_missing()
    {
        var service =
            new FakeVacancyService();

        var controller =
            CreateControllerWithoutUser(service);

        var result =
            await controller.GetOwnVacancies(
                new EmployerVacancyListRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);

        Assert.Null(
            service.LastListEmployerUserId);
    }

    [Fact]
    public async Task GetOwnVacancy_returns_200_for_success()
    {
        var employerUserId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        var vacancy =
            CreateVacancyDto(vacancyId);

        var service =
            new FakeVacancyService
            {
                DetailResult = vacancy
            };

        var controller =
            CreateController(
                service,
                employerUserId);

        var result =
            await controller.GetOwnVacancy(
                vacancyId,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<VacancyDto>(
                ok.Value);

        Assert.Equal(
            vacancyId,
            response.Id);

        Assert.Equal(
            employerUserId,
            service.LastDetailEmployerUserId);

        Assert.Equal(
            vacancyId,
            service.LastDetailVacancyId);
    }

    [Fact]
    public async Task GetOwnVacancy_returns_404_when_missing()
    {
        var service =
            new FakeVacancyService
            {
                DetailResult = null
            };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.GetOwnVacancy(
                Guid.NewGuid(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_200_for_success()
    {
        var employerUserId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        var vacancy =
            CreateVacancyDto(vacancyId);

        var service =
            new FakeVacancyService
            {
                UpdateResult =
                    VacancyUpdateResult.Success(
                        vacancy)
            };

        var controller =
            CreateController(
                service,
                employerUserId);

        var request =
            CreateUpdateRequest();

        var result =
            await controller.UpdateOwnVacancy(
                vacancyId,
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<VacancyDto>(
                ok.Value);

        Assert.Equal(
            vacancyId,
            response.Id);

        Assert.Equal(
            employerUserId,
            service.LastUpdateEmployerUserId);

        Assert.Equal(
            vacancyId,
            service.LastUpdateVacancyId);

        Assert.Same(
            request,
            service.LastUpdateRequest);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_400_for_invalid_input()
    {
        await AssertUpdateFailureAsync(
            VacancyUpdateFailureReason.InvalidInput,
            StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_400_for_invalid_required_skills()
    {
        await AssertUpdateFailureAsync(
            VacancyUpdateFailureReason.InvalidRequiredSkills,
            StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_404_for_not_found()
    {
        await AssertUpdateFailureAsync(
            VacancyUpdateFailureReason.NotFound,
            StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_409_for_closed()
    {
        await AssertUpdateFailureAsync(
            VacancyUpdateFailureReason.Closed,
            StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_409_for_concurrency_conflict()
    {
        await AssertUpdateFailureAsync(
            VacancyUpdateFailureReason.ConcurrencyConflict,
            StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_500_for_persistence_failure()
    {
        await AssertUpdateFailureAsync(
            VacancyUpdateFailureReason.PersistenceFailed,
            StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task UpdateOwnVacancy_returns_401_when_sub_claim_missing()
    {
        var service =
            new FakeVacancyService();

        var controller =
            CreateControllerWithoutUser(
                service);

        var result =
            await controller.UpdateOwnVacancy(
                Guid.NewGuid(),
                CreateUpdateRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);

        Assert.Null(
            service.LastUpdateEmployerUserId);

        Assert.Null(
            service.LastUpdateVacancyId);

        Assert.Null(
            service.LastUpdateRequest);
    }

    private static async Task AssertUpdateFailureAsync(
        VacancyUpdateFailureReason failureReason,
        int expectedStatusCode)
    {
        var service =
            new FakeVacancyService
            {
                UpdateResult =
                    VacancyUpdateResult.Failure(
                        failureReason)
            };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.UpdateOwnVacancy(
                Guid.NewGuid(),
                CreateUpdateRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            expectedStatusCode,
            problem.StatusCode);
    }

    private static VacancyDto CreateVacancyDto(
        Guid vacancyId)
    {
        return new VacancyDto(
            vacancyId,
            "Backend Developer",
            "Build and maintain secure backend application services.",
            "Colombo",
            24,
            EducationLevel.Bachelor,
            VacancyStatus.Open,
            Utc(12),
            Utc(13),
            null,
            new[]
            {
                new SkillSummaryDto(
                    Guid.NewGuid(),
                    "C#")
            },
            RowVersion());
    }

    private static UpdateVacancyRequest CreateUpdateRequest()
    {
        return new UpdateVacancyRequest(
            "Senior Backend Developer",
            "Design and maintain secure backend services and APIs.",
            "Kandy",
            36,
            EducationLevel.Bachelor,
            new[]
            {
                Guid.NewGuid()
            },
            RowVersion());
    }

    private static byte[] RowVersion() =>
        new byte[]
        {
            1, 2, 3, 4,
            5, 6, 7, 8
        };

    private static DateTime Utc(int hour) =>
        new(
            2026,
            9,
            11,
            hour,
            0,
            0,
            DateTimeKind.Utc);

    private static VacanciesController CreateController(
        FakeVacancyService service,
        Guid employerUserId)
    {
        return CreateControllerWithSub(
            service,
            employerUserId.ToString());
    }

    private static VacanciesController
        CreateControllerWithoutUser(
            FakeVacancyService service)
    {
        var controller =
            new VacanciesController(service);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                new ClaimsIdentity())
                    }
            };

        return controller;
    }

    private static VacanciesController CreateControllerWithSub(
        FakeVacancyService service,
        string sub)
    {
        var identity =
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        "sub",
                        sub),

                    new Claim(
                        "role",
                        RoleNames.Employer)
                },
                authenticationType: "Test");

        var controller =
            new VacanciesController(service);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(identity)
                    }
            };

        return controller;
    }

    private sealed class FakeVacancyService
        : IVacancyService
    {
        public EmployerVacancyPageDto? ListResult { get; set; }

        public VacancyDto? DetailResult { get; set; }

        public VacancyUpdateResult UpdateResult { get; set; } =
            VacancyUpdateResult.Failure(
                VacancyUpdateFailureReason.NotFound);

        public Guid? LastListEmployerUserId { get; private set; }

        public EmployerVacancyListRequest? LastListRequest { get; private set; }

        public Guid? LastDetailEmployerUserId { get; private set; }

        public Guid? LastDetailVacancyId { get; private set; }

        public Guid? LastUpdateEmployerUserId { get; private set; }

        public Guid? LastUpdateVacancyId { get; private set; }

        public UpdateVacancyRequest? LastUpdateRequest { get; private set; }

        public Task<EmployerVacancyPageDto?> GetOwnVacanciesAsync(
            Guid employerUserId,
            EmployerVacancyListRequest request,
            CancellationToken cancellationToken = default)
        {
            LastListEmployerUserId =
                employerUserId;

            LastListRequest =
                request;

            return Task.FromResult(
                ListResult);
        }

        public Task<VacancyDto?> GetOwnVacancyAsync(
            Guid employerUserId,
            Guid vacancyId,
            CancellationToken cancellationToken = default)
        {
            LastDetailEmployerUserId =
                employerUserId;

            LastDetailVacancyId =
                vacancyId;

            return Task.FromResult(
                DetailResult);
        }

        public Task<VacancyCreateResult> CreateOwnVacancyAsync(
            Guid employerUserId,
            CreateVacancyRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                VacancyCreateResult.Failure(
                    VacancyCreateFailureReason.InvalidInput));
        }

        public Task<VacancyUpdateResult> UpdateOwnVacancyAsync(
            Guid employerUserId,
            Guid vacancyId,
            UpdateVacancyRequest request,
            CancellationToken cancellationToken = default)
        {
            LastUpdateEmployerUserId =
                employerUserId;

            LastUpdateVacancyId =
                vacancyId;

            LastUpdateRequest =
                request;

            return Task.FromResult(
                UpdateResult);
        }

        public Task<VacancyStatusUpdateResult>
            UpdateOwnVacancyStatusAsync(
                Guid employerUserId,
                Guid vacancyId,
                UpdateVacancyStatusRequest request,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                VacancyStatusUpdateResult.Failure(
                    VacancyStatusUpdateFailureReason.NotFound));
        }
    }
}