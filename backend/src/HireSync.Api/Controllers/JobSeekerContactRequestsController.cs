using HireSync.Application.DTOs.ContactRequests;
using HireSync.Application.Interfaces.ContactRequests;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/job-seeker/contact-requests")]
[Authorize(Roles = RoleNames.JobSeeker)]
public sealed class JobSeekerContactRequestsController
    : ControllerBase
{
    private readonly IContactRequestService
        _contactRequestService;

    private readonly ICurrentUser _currentUser;

    public JobSeekerContactRequestsController(
        IContactRequestService contactRequestService,
        ICurrentUser currentUser)
    {
        _contactRequestService =
            contactRequestService;

        _currentUser =
            currentUser;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<JobSeekerContactRequestDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<JobSeekerContactRequestDto>>>
        GetContactRequests(
            CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated ||
            !_currentUser.UserId.HasValue ||
            _currentUser.UserId.Value == Guid.Empty)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "Invalid authenticated user",
                detail:
                    "The authenticated Job Seeker identifier is invalid.");
        }

        var contactRequests =
            await _contactRequestService
                .GetForOwnJobSeekerAsync(
                    _currentUser.UserId.Value,
                    cancellationToken);

        return Ok(contactRequests);
    }

    [HttpPatch("{contactRequestId:guid}/status")]
    [ProducesResponseType(
        typeof(ContactRequestDto),
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
    public async Task<ActionResult<ContactRequestDto>>
        RespondToContactRequest(
            [FromRoute] Guid contactRequestId,
            [FromBody] RespondContactRequestRequest request,
            CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated ||
            !_currentUser.UserId.HasValue ||
            _currentUser.UserId.Value == Guid.Empty)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "Invalid authenticated user",
                detail:
                    "The authenticated Job Seeker identifier is invalid.");
        }

        var result =
            await _contactRequestService
                .RespondToOwnContactRequestAsync(
                    _currentUser.UserId.Value,
                    contactRequestId,
                    request,
                    cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.ContactRequest);
        }

        return result.FailureReason switch
        {
            ContactRequestWriteFailureReason.InvalidInput =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid contact response",
                    detail:
                        "The submitted contact response is invalid."),

            ContactRequestWriteFailureReason.NotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "Contact request not found",
                    detail:
                        "The contact request could not be found."),

            ContactRequestWriteFailureReason.ParticipantInactive =>
                Problem(
                    statusCode:
                        StatusCodes.Status403Forbidden,
                    title:
                        "Contact response is not allowed",
                    detail:
                        "The Job Seeker account must be Active."),

            ContactRequestWriteFailureReason.InvalidTransition =>
                Problem(
                    statusCode:
                        StatusCodes.Status409Conflict,
                    title:
                        "Invalid contact request transition",
                    detail:
                        "Only a Pending request can be accepted or declined."),

            ContactRequestWriteFailureReason.ConcurrencyConflict =>
                Problem(
                    statusCode:
                        StatusCodes.Status409Conflict,
                    title:
                        "Contact request was modified",
                    detail:
                        "The contact request has changed since it was loaded. Reload it and try again."),

            ContactRequestWriteFailureReason.PersistenceFailed =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Contact response failed",
                    detail:
                        "The contact response could not be saved."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Contact response failed")
        };
    }
}
