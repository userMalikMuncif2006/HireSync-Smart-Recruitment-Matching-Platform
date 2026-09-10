using System.Security.Claims;
using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Employer;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public class VacanciesControllerContractTests
{
    [Fact]
    public void Controller_requires_Employer_role()
    {
        var attribute = typeof(VacanciesController)
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            RoleNames.Employer,
            attribute.Roles);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_200_for_success()
    {
        var employerUserId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        var vacancy = CreateVacancyStatusDto(vacancyId);

        var service = new FakeVacancyService
        {
            Result =
                VacancyStatusUpdateResult.Success(vacancy)
        };

        var controller =
            CreateController(
                service,
                employerUserId);

        var request =
            CreateValidRequest();

        var result =
            await controller.UpdateOwnVacancyStatus(
                vacancyId,
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<VacancyStatusDto>(
                ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(vacancyId, response.Id);
        Assert.Equal(
            VacancyStatus.Closed,
            response.Status);
        Assert.NotNull(response.ClosedAtUtc);
        Assert.NotEmpty(response.RowVersion);

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
    public async Task UpdateOwnVacancyStatus_returns_400_for_invalid_input()
    {
        var service = new FakeVacancyService
        {
            Result =
                VacancyStatusUpdateResult.Failure(
                    VacancyStatusUpdateFailureReason.InvalidInput)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.UpdateOwnVacancyStatus(
                Guid.NewGuid(),
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(400, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_404_for_not_found()
    {
        var service = new FakeVacancyService
        {
            Result =
                VacancyStatusUpdateResult.Failure(
                    VacancyStatusUpdateFailureReason.NotFound)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.UpdateOwnVacancyStatus(
                Guid.NewGuid(),
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(404, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_409_when_already_closed()
    {
        var service = new FakeVacancyService
        {
            Result =
                VacancyStatusUpdateResult.Failure(
                    VacancyStatusUpdateFailureReason.AlreadyClosed)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.UpdateOwnVacancyStatus(
                Guid.NewGuid(),
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_409_for_concurrency_conflict()
    {
        var service = new FakeVacancyService
        {
            Result =
                VacancyStatusUpdateResult.Failure(
                    VacancyStatusUpdateFailureReason.ConcurrencyConflict)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.UpdateOwnVacancyStatus(
                Guid.NewGuid(),
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_500_for_persistence_failure()
    {
        var service = new FakeVacancyService
        {
            Result =
                VacancyStatusUpdateResult.Failure(
                    VacancyStatusUpdateFailureReason.PersistenceFailed)
        };

        var controller =
            CreateController(
                service,
                Guid.NewGuid());

        var result =
            await controller.UpdateOwnVacancyStatus(
                Guid.NewGuid(),
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(500, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_401_when_sub_claim_missing()
    {
        var service =
            new FakeVacancyService();

        var controller =
            CreateControllerWithoutUser(service);

        var result =
            await controller.UpdateOwnVacancyStatus(
                Guid.NewGuid(),
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(401, problem.StatusCode);
        Assert.Null(service.LastEmployerUserId);
        Assert.Null(service.LastVacancyId);
        Assert.Null(service.LastRequest);
    }

    [Fact]
    public async Task UpdateOwnVacancyStatus_returns_401_when_sub_claim_invalid()
    {
        var service =
            new FakeVacancyService();

        var controller =
            CreateControllerWithSub(
                service,
                "not-a-guid");

        var result =
            await controller.UpdateOwnVacancyStatus(
                Guid.NewGuid(),
                CreateValidRequest(),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(401, problem.StatusCode);
        Assert.Null(service.LastEmployerUserId);
        Assert.Null(service.LastVacancyId);
        Assert.Null(service.LastRequest);
    }

    private static UpdateVacancyStatusRequest
        CreateValidRequest()
    {
        return new UpdateVacancyStatusRequest(
            Status: VacancyStatus.Closed,
            RowVersion:
            [
                1, 2, 3, 4,
                5, 6, 7, 8
            ]);
    }

    private static VacancyStatusDto
        CreateVacancyStatusDto(Guid vacancyId)
    {
        return new VacancyStatusDto(
            Id: vacancyId,
            Status: VacancyStatus.Closed,
            ClosedAtUtc:
                new DateTime(
                    2026,
                    9,
                    10,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc),
            RowVersion:
            [
                8, 7, 6, 5,
                4, 3, 2, 1
            ]);
    }

    private static VacanciesController
        CreateController(
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

    private static VacanciesController
        CreateControllerWithSub(
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
                            new ClaimsPrincipal(
                                identity)
                    }
            };

        return controller;
    }

    private sealed class FakeVacancyService
        : IVacancyService
    {
        public VacancyStatusUpdateResult Result { get; set; } =
            VacancyStatusUpdateResult.Failure(
                VacancyStatusUpdateFailureReason.NotFound);

        public Guid? LastEmployerUserId { get; private set; }

        public Guid? LastVacancyId { get; private set; }

        public UpdateVacancyStatusRequest? LastRequest { get; private set; }

        public Task<VacancyStatusUpdateResult>
            UpdateOwnVacancyStatusAsync(
                Guid employerUserId,
                Guid vacancyId,
                UpdateVacancyStatusRequest request,
                CancellationToken cancellationToken = default)
        {
            LastEmployerUserId = employerUserId;
            LastVacancyId = vacancyId;
            LastRequest = request;

            return Task.FromResult(Result);
        }
    }
}