using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Admin;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public class AdminControllerContractTests
{
    [Fact]
    public void Controller_requires_Administrator_role()
    {
        var attribute = typeof(AdminController)
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            RoleNames.Administrator,
            attribute.Roles);
    }

    [Fact]
    public async Task GetPendingEmployers_returns_200()
    {
        var employer = new EmployerVerificationSummaryDto(
            Guid.NewGuid(),
            "employer@example.com",
            "Test Employer",
            EmployerVerificationStatus.Pending);

        var service = new FakeEmployerVerificationAdminService
        {
            PendingEmployers = new[] { employer }
        };

        var controller = CreateController(service);

        var result = await controller.GetPendingEmployers(
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var employers =
            Assert.IsAssignableFrom<
                IReadOnlyList<EmployerVerificationSummaryDto>>(
                    ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Single(employers);
    }

    [Fact]
    public async Task ApproveEmployer_returns_200_for_success()
    {
        var employerId = Guid.NewGuid();

        var employer = new EmployerVerificationSummaryDto(
            employerId,
            "employer@example.com",
            "Test Employer",
            EmployerVerificationStatus.Approved);

        var service = new FakeEmployerVerificationAdminService
        {
            ApproveResult =
                EmployerVerificationUpdateResult.Success(employer)
        };

        var controller = CreateController(service);

        var result = await controller.ApproveEmployer(
            employerId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response =
            Assert.IsType<EmployerVerificationSummaryDto>(ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(
            EmployerVerificationStatus.Approved,
            response.Status);
    }

    [Fact]
    public async Task RejectEmployer_returns_200_for_success()
    {
        var employerId = Guid.NewGuid();

        var employer = new EmployerVerificationSummaryDto(
            employerId,
            "employer@example.com",
            "Test Employer",
            EmployerVerificationStatus.Rejected);

        var service = new FakeEmployerVerificationAdminService
        {
            RejectResult =
                EmployerVerificationUpdateResult.Success(employer)
        };

        var controller = CreateController(service);

        var result = await controller.RejectEmployer(
            employerId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response =
            Assert.IsType<EmployerVerificationSummaryDto>(ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(
            EmployerVerificationStatus.Rejected,
            response.Status);
    }

    [Fact]
    public async Task ApproveEmployer_returns_404_when_not_found()
    {
        var service = new FakeEmployerVerificationAdminService
        {
            ApproveResult =
                EmployerVerificationUpdateResult.Failure(
                    EmployerVerificationUpdateFailureReason.NotFound)
        };

        var controller = CreateController(service);

        var result = await controller.ApproveEmployer(
            Guid.NewGuid(),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(404, problem.StatusCode);
    }

    [Fact]
    public async Task ApproveEmployer_returns_409_for_invalid_transition()
    {
        var service = new FakeEmployerVerificationAdminService
        {
            ApproveResult =
                EmployerVerificationUpdateResult.Failure(
                    EmployerVerificationUpdateFailureReason.InvalidTransition)
        };

        var controller = CreateController(service);

        var result = await controller.ApproveEmployer(
            Guid.NewGuid(),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(409, problem.StatusCode);
    }

    [Fact]
    public async Task ApproveEmployer_returns_500_when_persistence_fails()
    {
        var service = new FakeEmployerVerificationAdminService
        {
            ApproveResult =
                EmployerVerificationUpdateResult.Failure(
                    EmployerVerificationUpdateFailureReason.PersistenceFailed)
        };

        var controller = CreateController(service);

        var result = await controller.ApproveEmployer(
            Guid.NewGuid(),
            CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(500, problem.StatusCode);
    }

    private static AdminController CreateController(
        FakeEmployerVerificationAdminService service)
    {
        var controller = new AdminController(service);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }
}
