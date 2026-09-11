using HireSync.Application.DTOs.Employer;
using HireSync.Application.Interfaces.Employer;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/employer/profile")]
[Authorize(Roles = RoleNames.Employer)]
public sealed class EmployerProfileController : ControllerBase
{
    private readonly IEmployerProfileService _employerProfileService;

    public EmployerProfileController(
        IEmployerProfileService employerProfileService)
    {
        _employerProfileService = employerProfileService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(EmployerProfileDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployerProfileDto>> GetOwnProfile(
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
                detail: "The authenticated user identifier is invalid.");
        }

        var profile =
            await _employerProfileService.GetOwnProfileAsync(
                employerUserId,
                cancellationToken);

        if (profile is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Employer profile not found",
                detail: "An Employer profile could not be found for the authenticated user.");
        }

        return Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(
        typeof(EmployerProfileDto),
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
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployerProfileDto>> UpdateOwnProfile(
        [FromBody] UpdateEmployerProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployerUserId(out var employerUserId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid authenticated user",
                detail: "The authenticated user identifier is invalid.");
        }

        var result =
            await _employerProfileService.UpdateOwnProfileAsync(
                employerUserId,
                request,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Profile);
        }

        return result.FailureReason switch
        {
            EmployerProfileUpdateFailureReason.InvalidInput =>
                Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Employer profile",
                    detail: "The submitted Employer profile data is invalid."),

            EmployerProfileUpdateFailureReason.NotFound =>
                Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Employer profile not found",
                    detail: "An Employer profile could not be found for the authenticated user."),

            EmployerProfileUpdateFailureReason
                    .DuplicateBusinessRegistrationNumber =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Business Registration Number already exists",
                    detail: "Another Employer profile already uses this Business Registration Number."),

            EmployerProfileUpdateFailureReason
                    .VerificationResetRequired =>
                Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Company re-verification required",
                    detail: "This company identity change requires Employer verification to return to Pending before it can be completed."),

            EmployerProfileUpdateFailureReason.PersistenceFailed =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Employer profile update failed",
                    detail: "The Employer profile could not be saved."),

            _ =>
                Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Employer profile update failed")
        };
    }

    private bool TryGetEmployerUserId(
        out Guid employerUserId)
    {
        var value = User.FindFirst("sub")?.Value;

        return Guid.TryParse(
            value,
            out employerUserId);
    }
}