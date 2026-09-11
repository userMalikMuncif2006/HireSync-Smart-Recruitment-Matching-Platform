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

public sealed class VacanciesControllerCreateContractTests
{
    [Fact]
    public async Task CreateOwnVacancy_returns_201_for_success()
    {
        var employerUserId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        var vacancy =
            new VacancyDto(
                vacancyId,
                "Backend Developer",
                "Build and maintain secure backend application services.",
                "Colombo",
                24,
                EducationLevel.Bachelor,
                VacancyStatus.Open,
                new DateTime(
                    2026,
                    9,
                    11,
                    14,
                    0,
                    0,
                    DateTimeKind.Utc),
                new DateTime(
                    2026,
                    9,
                    11,
                    14,
                    0,
                    0,
                    DateTimeKind.Utc),
                null,
                new[]
                {
                    new SkillSummaryDto(
                        Guid.NewGuid(),
                        "C#")
                },
                new byte[] { 1, 2, 3, 4 });

        var service =
            new FakeVacancyService
            {
                CreateResult =
                    VacancyCreateResult.Success(vacancy)
            };

        var controller =
            CreateController(
                service,
                employerUserId);

        var request =
            CreateValidRequest();

        var result =
            await controller.CreateOwnVacancy(
                request,
                CancellationToken.None);

        var created =
            Assert.IsType<CreatedResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            created.StatusCode);

        Assert.Equal(
            $"/api/v1/employer/vacancies/{vacancyId}",
            created.Location);

        var response =
            Assert.IsType<VacancyDto>(
                created.Value);

        Assert.Equal(
            vacancyId,
            response.Id);

        Assert.Equal(
            employerUserId,
            service.LastCreateEmployerUserId);

        Assert.Same(
            request,
            service.LastCreateRequest);
    }

    [Fact]
    public async Task CreateOwnVacancy_returns_400_for_invalid_input()
    {
        var service =
            new FakeVacancyService
            {
                CreateResult =
                    VacancyCreateResult.Failure(
                        VacancyCreateFailureReason.InvalidInput)
            };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.CreateOwnVacancy(
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem.StatusCode);
    }

    [Fact]
    public async Task CreateOwnVacancy_returns_400_for_invalid_required_skills()
    {
        var service =
            new FakeVacancyService
            {
                CreateResult =
                    VacancyCreateResult.Failure(
                        VacancyCreateFailureReason.InvalidRequiredSkills)
            };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.CreateOwnVacancy(
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem.StatusCode);
    }

    [Fact]
    public async Task CreateOwnVacancy_returns_403_when_employer_not_ready()
    {
        var service =
            new FakeVacancyService
            {
                CreateResult =
                    VacancyCreateResult.Failure(
                        VacancyCreateFailureReason.EmployerNotReady)
            };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.CreateOwnVacancy(
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            problem.StatusCode);
    }

    [Fact]
    public async Task CreateOwnVacancy_returns_500_for_persistence_failure()
    {
        var service =
            new FakeVacancyService
            {
                CreateResult =
                    VacancyCreateResult.Failure(
                        VacancyCreateFailureReason.PersistenceFailed)
            };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.CreateOwnVacancy(
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            problem.StatusCode);
    }

    [Fact]
    public async Task CreateOwnVacancy_returns_401_when_sub_claim_missing()
    {
        var service =
            new FakeVacancyService();

        var controller =
            CreateControllerWithoutUser(
                service);

        var result =
            await controller.CreateOwnVacancy(
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);

        Assert.Null(
            service.LastCreateEmployerUserId);

        Assert.Null(
            service.LastCreateRequest);
    }

    [Fact]
    public async Task CreateOwnVacancy_returns_401_when_sub_claim_invalid()
    {
        var service =
            new FakeVacancyService();

        var controller =
            CreateControllerWithSub(
                service,
                "not-a-guid");

        var result =
            await controller.CreateOwnVacancy(
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);

        Assert.Null(
            service.LastCreateEmployerUserId);

        Assert.Null(
            service.LastCreateRequest);
    }

    private static CreateVacancyRequest CreateValidRequest()
    {
        return new CreateVacancyRequest(
            "Backend Developer",
            "Build and maintain secure backend application services.",
            "Colombo",
            24,
            EducationLevel.Bachelor,
            new[]
            {
                Guid.NewGuid()
            });
    }

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
                    new Claim("sub", sub),
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
        public VacancyCreateResult CreateResult { get; set; } =
            VacancyCreateResult.Failure(
                VacancyCreateFailureReason.InvalidInput);

        public Guid? LastCreateEmployerUserId { get; private set; }

        public CreateVacancyRequest? LastCreateRequest { get; private set; }

        public Task<EmployerVacancyPageDto?> GetOwnVacanciesAsync(
            Guid employerUserId,
            EmployerVacancyListRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<EmployerVacancyPageDto?>(null);
        }

        public Task<VacancyDto?> GetOwnVacancyAsync(
            Guid employerUserId,
            Guid vacancyId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<VacancyDto?>(null);
        }

        public Task<VacancyUpdateResult> UpdateOwnVacancyAsync(
            Guid employerUserId,
            Guid vacancyId,
            UpdateVacancyRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                VacancyUpdateResult.Failure(
                    VacancyUpdateFailureReason.NotFound));
        }

        public Task<VacancyCreateResult> CreateOwnVacancyAsync(
            Guid employerUserId,
            CreateVacancyRequest request,
            CancellationToken cancellationToken = default)
        {
            LastCreateEmployerUserId =
                employerUserId;

            LastCreateRequest =
                request;

            return Task.FromResult(
                CreateResult);
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