using System.Reflection;
using HireSync.Api.Controllers;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class JobSeekerApplicationTrackingControllerContractTests
{
    [Fact]
    public void Controller_uses_canonical_tracking_route()
    {
        var route =
            typeof(JobSeekerApplicationTrackingController)
                .GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);

        Assert.Equal(
            "api/v1/job-seeker/applications",
            route!.Template);
    }

    [Fact]
    public void Controller_is_job_seeker_only()
    {
        var authorize =
            typeof(JobSeekerApplicationTrackingController)
                .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);

        Assert.Equal(
            RoleNames.JobSeeker,
            authorize!.Roles);
    }

    [Fact]
    public void Tracking_action_is_http_get()
    {
        var method =
            typeof(JobSeekerApplicationTrackingController)
                .GetMethod(
                    nameof(
                        JobSeekerApplicationTrackingController
                            .GetApplications));

        Assert.NotNull(method);

        var httpGet =
            method!
                .GetCustomAttribute<HttpGetAttribute>();

        Assert.NotNull(httpGet);
        Assert.Null(httpGet!.Template);
    }
}
