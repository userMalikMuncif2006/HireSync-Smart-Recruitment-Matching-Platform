using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class AdminDashboardControllerContractTests
{
    [Fact]
    public void Controller_requires_Administrator_role()
    {
        var attribute =
            typeof(AdminDashboardController)
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
    public async Task GetDashboard_returns_200_with_service_result()
    {
        var calculatedAtUtc =
            new DateTime(
                2026,
                9,
                13,
                5,
                0,
                0,
                DateTimeKind.Utc);

        var expected =
            new AdminDashboardDto(
                7,
                4,
                9,
                calculatedAtUtc);

        var controller =
            new AdminDashboardController(
                new FakeAdminDashboardService(expected));

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        var result =
            await controller.GetDashboard(
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<AdminDashboardDto>(
                ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(expected, response);
    }

    private sealed class FakeAdminDashboardService
        : IAdminDashboardService
    {
        private readonly AdminDashboardDto _dashboard;

        public FakeAdminDashboardService(
            AdminDashboardDto dashboard)
        {
            _dashboard = dashboard;
        }

        public Task<AdminDashboardDto> GetAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_dashboard);
        }
    }
}