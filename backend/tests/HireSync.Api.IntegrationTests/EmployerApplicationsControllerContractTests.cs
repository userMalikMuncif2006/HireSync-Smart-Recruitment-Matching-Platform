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

public sealed class EmployerApplicationsControllerContractTests
{
    [Fact]
    public void Controller_requires_Employer_role()
    {
        var authorize =
            typeof(EmployerApplicationsController)
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
    public void Controller_uses_canonical_status_route()
    {
        var route =
            typeof(EmployerApplicationsController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit: true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/employer/applications",
            route.Template);

        var method =
            typeof(EmployerApplicationsController)
                .GetMethod(
                    nameof(
                        EmployerApplicationsController
                            .UpdateApplicationStatus));

        Assert.NotNull(method);

        var patch =
            method!
                .GetCustomAttributes(
                    typeof(HttpPatchAttribute),
                    inherit: true)
                .Cast<HttpPatchAttribute>()
                .Single();

        Assert.Equal(
            "{applicationId:guid}/status",
            patch.Template);
    }

    [Fact]
    public async Task Update_returns_200_for_success()
    {
        var employerUserId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        var request =
            new UpdateApplicationStatusRequest(
                ApplicationStatus.UnderReview,
                new byte[] { 1 });

        var expected =
            new ApplicationStatusDto(
                applicationId,
                ApplicationStatus.UnderReview,
                DateTime.UtcNow,
                new byte[] { 2 });

        var service =
            new FakeApplicationStatusService(
                ApplicationStatusUpdateResult
                    .Success(expected));

        var controller =
            new EmployerApplicationsController(
                service,
                new FakeCurrentUser(
                    employerUserId));

        var action =
            await controller.UpdateApplicationStatus(
                applicationId,
                request,
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
            employerUserId,
            service.LastEmployerUserId);

        Assert.Equal(
            applicationId,
            service.LastApplicationId);

        Assert.Same(
            request,
            service.LastRequest);
    }

    [Fact]
    public async Task Update_returns_401_for_invalid_current_user()
    {
        var controller =
            new EmployerApplicationsController(
                new FakeApplicationStatusService(
                    ApplicationStatusUpdateResult.Failure(
                        ApplicationStatusUpdateFailureReason
                            .InvalidInput)),
                new FakeCurrentUser(null));

        var action =
            await controller.UpdateApplicationStatus(
                Guid.NewGuid(),
                new UpdateApplicationStatusRequest(
                    ApplicationStatus.UnderReview,
                    new byte[] { 1 }),
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
        ApplicationStatusUpdateFailureReason.InvalidInput,
        StatusCodes.Status400BadRequest)]
    [InlineData(
        ApplicationStatusUpdateFailureReason.NotFound,
        StatusCodes.Status404NotFound)]
    [InlineData(
        ApplicationStatusUpdateFailureReason.InvalidTransition,
        StatusCodes.Status409Conflict)]
    [InlineData(
        ApplicationStatusUpdateFailureReason.ConcurrencyConflict,
        StatusCodes.Status409Conflict)]
    [InlineData(
        ApplicationStatusUpdateFailureReason.PersistenceFailed,
        StatusCodes.Status500InternalServerError)]
    public async Task Update_maps_failure_to_expected_status(
        ApplicationStatusUpdateFailureReason failureReason,
        int expectedStatus)
    {
        var controller =
            new EmployerApplicationsController(
                new FakeApplicationStatusService(
                    ApplicationStatusUpdateResult
                        .Failure(failureReason)),
                new FakeCurrentUser(
                    Guid.NewGuid()));

        var action =
            await controller.UpdateApplicationStatus(
                Guid.NewGuid(),
                new UpdateApplicationStatusRequest(
                    ApplicationStatus.UnderReview,
                    new byte[] { 1 }),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            expectedStatus,
            problem.StatusCode);
    }

    private sealed class FakeApplicationStatusService
        : IApplicationStatusService
    {
        private readonly ApplicationStatusUpdateResult
            _result;

        public FakeApplicationStatusService(
            ApplicationStatusUpdateResult result)
        {
            _result = result;
        }

        public Guid? LastEmployerUserId { get; private set; }

        public Guid? LastApplicationId { get; private set; }

        public UpdateApplicationStatusRequest?
            LastRequest { get; private set; }

        public Task<ApplicationStatusUpdateResult>
            UpdateOwnApplicationStatusAsync(
                Guid employerUserId,
                Guid applicationId,
                UpdateApplicationStatusRequest request,
                CancellationToken cancellationToken = default)
        {
            LastEmployerUserId = employerUserId;
            LastApplicationId = applicationId;
            LastRequest = request;

            return Task.FromResult(_result);
        }
    }

    private sealed class FakeCurrentUser
        : ICurrentUser
    {
        public FakeCurrentUser(
            Guid? userId)
        {
            UserId = userId;
        }

        public bool IsAuthenticated =>
            UserId.HasValue;

        public Guid? UserId { get; }

        public string? Role =>
            RoleNames.Employer;

        public string? Email =>
            "employer@example.com";
    }
}
