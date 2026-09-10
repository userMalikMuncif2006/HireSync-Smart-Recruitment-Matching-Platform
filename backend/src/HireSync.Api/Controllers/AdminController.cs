using HireSync.Application.DTOs.Admin;
using HireSync.Application.Interfaces.Admin;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = RoleNames.Administrator)]
public sealed class AdminController : ControllerBase
{
    private readonly IEmployerVerificationAdminService
        _employerVerificationService;

    public AdminController(
        IEmployerVerificationAdminService employerVerificationService)
    {
        _employerVerificationService = employerVerificationService;
    }

    [HttpGet("employers/pending")]
    [ProducesResponseType(
        typeof(IReadOnlyList<EmployerVerificationSummaryDto>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyList<EmployerVerificationSummaryDto>>>
        GetPendingEmployers(
            CancellationToken cancellationToken)
    {
        var employers =
            await _employerVerificationService
                .GetPendingEmployersAsync(cancellationToken);

        return Ok(employers);
    }

    [HttpPatch("employers/{employerUserId:guid}/approve")]
    [ProducesResponseType(
        typeof(EmployerVerificationSummaryDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployerVerificationSummaryDto>>
        ApproveEmployer(
            Guid employerUserId,
            CancellationToken cancellationToken)
    {
        var result =
            await _employerVerificationService.ApproveAsync(
                employerUserId,
                cancellationToken);

        return MapUpdateResult(result);
    }

    [HttpPatch("employers/{employerUserId:guid}/reject")]
    [ProducesResponseType(
        typeof(EmployerVerificationSummaryDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployerVerificationSummaryDto>>
        RejectEmployer(
            Guid employerUserId,
            CancellationToken cancellationToken)
    {
        var result =
            await _employerVerificationService.RejectAsync(
                employerUserId,
                cancellationToken);

        return MapUpdateResult(result);
    }

    private ActionResult<EmployerVerificationSummaryDto>
        MapUpdateResult(
            EmployerVerificationUpdateResult result)
    {
        if (result.Succeeded && result.Employer is not null)
        {
            return Ok(result.Employer);
        }

        return result.FailureReason switch
        {
            EmployerVerificationUpdateFailureReason.NotFound =>
                Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Employer not found",
                    detail:
                        "The Employer account could not be found."),

            EmployerVerificationUpdateFailureReason.InvalidTransition =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Invalid verification transition",
                    detail:
                        "The Employer verification status can no longer be changed."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title: "Employer verification update failed",
                    detail:
                        "The Employer verification status could not be persisted.")
        };
    }
}
