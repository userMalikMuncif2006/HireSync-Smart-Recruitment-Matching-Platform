using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Security;
using HireSync.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class AdminUsersControllerContractTests
{
    [Fact]
    public void Controller_requires_Administrator_role()
    {
        var attribute =
            typeof(AdminUsersController)
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
    public async Task GetUsers_returns_200_with_service_result()
    {
        var expected =
            new AdminUserListDto(
                Array.Empty<AdminUserListItemDto>(),
                1,
                20,
                0);

        var service =
            new FakeAdminUserService(expected);

        var controller =
            CreateController(service);

        var result =
            await controller.GetUsers(
                search: null,
                status: null,
                page: 1,
                pageSize: 20,
                cancellationToken:
                    CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<AdminUserListDto>(
                ok.Value);

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal(expected, response);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetUsers_returns_400_for_invalid_paging(
        int page,
        int pageSize)
    {
        var service =
            new FakeAdminUserService(
                new AdminUserListDto(
                    Array.Empty<AdminUserListItemDto>(),
                    1,
                    20,
                    0));

        var controller =
            CreateController(service);

        var result =
            await controller.GetUsers(
                search: null,
                status: null,
                page: page,
                pageSize: pageSize,
                cancellationToken:
                    CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem.StatusCode);

        Assert.Equal(
            0,
            service.CallCount);
    }

    [Fact]
    public async Task GetUsers_returns_400_for_search_over_100_characters()
    {
        var service =
            new FakeAdminUserService(
                new AdminUserListDto(
                    Array.Empty<AdminUserListItemDto>(),
                    1,
                    20,
                    0));

        var controller =
            CreateController(service);

        var result =
            await controller.GetUsers(
                search: new string('x', 101),
                status: null,
                page: 1,
                pageSize: 20,
                cancellationToken:
                    CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problem.StatusCode);

        Assert.Equal(
            0,
            service.CallCount);
    }

    private static AdminUsersController CreateController(
        FakeAdminUserService service)
    {
        var controller =
            new AdminUsersController(service);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        return controller;
    }

    private sealed class FakeAdminUserService
        : IAdminUserService
    {
        private readonly AdminUserListDto _result;

        public FakeAdminUserService(
            AdminUserListDto result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<AdminUserListDto> GetUsersAsync(
            string? search,
            AccountStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(_result);
        }
    }
}