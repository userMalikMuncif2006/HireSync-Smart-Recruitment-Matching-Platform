using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.Skills;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/skills")]
[Authorize(
    Roles =
        RoleNames.JobSeeker + "," +
        RoleNames.Employer)]
public sealed class SkillsController
    : ControllerBase
{
    private readonly ISkillLookupService
        _skillLookupService;

    public SkillsController(
        ISkillLookupService skillLookupService)
    {
        _skillLookupService =
            skillLookupService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<SkillSummaryDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<IReadOnlyList<SkillSummaryDto>>>
        GetSkills(
            [FromQuery(Name = "query")]
            string? query,
            CancellationToken cancellationToken)
    {
        try
        {
            var skills =
                await _skillLookupService
                    .GetSkillsAsync(
                        query,
                        cancellationToken);

            return Ok(skills);
        }
        catch (ArgumentException)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title:
                    "Invalid skill lookup query",
                detail:
                    "The skill lookup query is invalid.");
        }
    }
}
