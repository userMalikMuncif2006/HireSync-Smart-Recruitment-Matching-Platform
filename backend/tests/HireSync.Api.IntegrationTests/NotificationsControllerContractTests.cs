using System.Reflection;
using HireSync.Api.Controllers;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class NotificationsControllerContractTests
{
    [Fact]
    public void Controller_uses_canonical_route_and_jobseeker_role()
    {
        var route =
            typeof(NotificationsController)
                .GetCustomAttribute<RouteAttribute>();

        var authorize =
            typeof(NotificationsController)
                .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(route);
        Assert.NotNull(authorize);

        Assert.Equal(
            "api/v1/notifications",
            route!.Template);

        Assert.Equal(
            RoleNames.JobSeeker,
            authorize!.Roles);
    }

    [Fact]
    public void List_action_is_http_get()
    {
        var method =
            typeof(NotificationsController)
                .GetMethod(
                    nameof(
                        NotificationsController
                            .GetNotifications));

        Assert.NotNull(method);

        var attribute =
            method!
                .GetCustomAttribute<HttpGetAttribute>();

        Assert.NotNull(attribute);
        Assert.Null(attribute!.Template);
    }

    [Fact]
    public void Read_actions_use_canonical_patch_routes()
    {
        var markRead =
            typeof(NotificationsController)
                .GetMethod(
                    nameof(
                        NotificationsController
                            .MarkRead));

        var markAll =
            typeof(NotificationsController)
                .GetMethod(
                    nameof(
                        NotificationsController
                            .MarkAllRead));

        Assert.NotNull(markRead);
        Assert.NotNull(markAll);

        var markReadPatch =
            markRead!
                .GetCustomAttribute<HttpPatchAttribute>();

        var markAllPatch =
            markAll!
                .GetCustomAttribute<HttpPatchAttribute>();

        Assert.NotNull(markReadPatch);
        Assert.NotNull(markAllPatch);

        Assert.Equal(
            "{notificationId:guid}/read",
            markReadPatch!.Template);

        Assert.Equal(
            "read-all",
            markAllPatch!.Template);
    }
}
