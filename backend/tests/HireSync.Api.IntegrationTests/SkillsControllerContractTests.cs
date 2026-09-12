using System.Reflection;
using HireSync.Api.Controllers;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class SkillsControllerContractTests
{
    [Fact]
    public void Controller_uses_canonical_skill_route()
    {
        var route =
            typeof(SkillsController)
                .GetCustomAttribute<
                    RouteAttribute>();

        Assert.NotNull(route);

        Assert.Equal(
            "api/v1/skills",
            route!.Template);
    }

    [Fact]
    public void Controller_allows_only_jobseeker_and_employer_roles()
    {
        var authorize =
            typeof(SkillsController)
                .GetCustomAttribute<
                    AuthorizeAttribute>();

        Assert.NotNull(authorize);

        Assert.Equal(
            RoleNames.JobSeeker + "," +
            RoleNames.Employer,
            authorize!.Roles);
    }

    [Fact]
    public void GetSkills_uses_http_get_and_query_parameter()
    {
        var method =
            typeof(SkillsController)
                .GetMethod(
                    nameof(
                        SkillsController.GetSkills));

        Assert.NotNull(method);

        var httpGet =
            method!
                .GetCustomAttribute<
                    HttpGetAttribute>();

        Assert.NotNull(httpGet);
        Assert.Null(httpGet!.Template);

        var queryParameter =
            method.GetParameters()
                .Single(
                    parameter =>
                        parameter.Name ==
                        "query");

        var fromQuery =
            queryParameter
                .GetCustomAttribute<
                    FromQueryAttribute>();

        Assert.NotNull(fromQuery);

        Assert.Equal(
            "query",
            fromQuery!.Name);

        Assert.Equal(
            typeof(string),
            queryParameter.ParameterType);
    }
}
