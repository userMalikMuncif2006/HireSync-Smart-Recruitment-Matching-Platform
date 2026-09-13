using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class AdminAccountStatusControllerContractTests
{
    [Fact]
    public void Controller_requires_Administrator_role()
    {
        var attribute =
            typeof(AdminAccountStatusController)
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
    public async Task UpdateStatus_returns_200_for_success()
    {
        var targetId =
            Guid.NewGuid();

        var dto =
            new AdminUserListItemDto(
                targetId,
                "Target User",
                "target@example.com",
                RoleNames.JobSeeker,
                AccountStatus.Suspended,
                DateTime.UtcNow,
                "AQID");

        var service =
            new FakeStatusService(
                AdminAccountStatusUpdateResult
                    .Success(dto));

        var controller =
            CreateController(
                service,
                authenticated: true);

        var result =
            await controller.UpdateStatus(
                targetId,
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Suspended,
                    new byte[] { 1, 2, 3 }),
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);
    }

    [Theory]
    [InlineData(
        AdminAccountStatusUpdateFailureReason.InvalidInput,
        StatusCodes.Status400BadRequest)]
    [InlineData(
        AdminAccountStatusUpdateFailureReason.NotFound,
        StatusCodes.Status404NotFound)]
    [InlineData(
        AdminAccountStatusUpdateFailureReason.ProtectedTarget,
        StatusCodes.Status403Forbidden)]
    [InlineData(
        AdminAccountStatusUpdateFailureReason.ConcurrencyConflict,
        StatusCodes.Status409Conflict)]
    [InlineData(
        AdminAccountStatusUpdateFailureReason.PersistenceFailed,
        StatusCodes.Status500InternalServerError)]
    public async Task UpdateStatus_maps_failure_reason(
        AdminAccountStatusUpdateFailureReason reason,
        int expectedStatus)
    {
        var service =
            new FakeStatusService(
                AdminAccountStatusUpdateResult
                    .Failure(reason));

        var controller =
            CreateController(
                service,
                authenticated: true);

        var result =
            await controller.UpdateStatus(
                Guid.NewGuid(),
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Suspended,
                    new byte[] { 1 }),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            expectedStatus,
            problem.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_returns_401_without_current_user()
    {
        var service =
            new FakeStatusService(
                AdminAccountStatusUpdateResult
                    .Failure(
                        AdminAccountStatusUpdateFailureReason
                            .InvalidInput));

        var controller =
            CreateController(
                service,
                authenticated: false);

        var result =
            await controller.UpdateStatus(
                Guid.NewGuid(),
                new UpdateAdminAccountStatusRequest(
                    AccountStatus.Suspended,
                    new byte[] { 1 }),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);

        Assert.Equal(
            0,
            service.CallCount);
    }

    private static AdminAccountStatusController
        CreateController(
            FakeStatusService service,
            bool authenticated)
    {
        var currentUser =
            new FakeCurrentUser(
                authenticated
                    ? Guid.NewGuid()
                    : null);

        var controller =
            new AdminAccountStatusController(
                service,
                currentUser);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        return controller;
    }

    private sealed class FakeStatusService
        : IAdminAccountStatusService
    {
        private readonly AdminAccountStatusUpdateResult
            _result;

        public FakeStatusService(
            AdminAccountStatusUpdateResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<AdminAccountStatusUpdateResult>
            UpdateStatusAsync(
                Guid administratorUserId,
                Guid targetUserId,
                UpdateAdminAccountStatusRequest request,
                CancellationToken cancellationToken = default)
        {
            CallCount++;

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
            UserId = userId;
        }

        public bool IsAuthenticated =>
            UserId.HasValue;

        public Guid? UserId { get; }

        public string? Role =>
            IsAuthenticated
                ? RoleNames.Administrator
                : null;

        public string? Email =>
            IsAuthenticated
                ? "admin@example.com"
                : null;
    }
}