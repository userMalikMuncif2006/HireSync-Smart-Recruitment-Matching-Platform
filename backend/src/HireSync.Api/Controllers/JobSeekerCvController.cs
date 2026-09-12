using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.JobSeeker;
using HireSync.Application.Security;
using HireSync.Application.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/job-seeker/cv")]
[Authorize(Roles = RoleNames.JobSeeker)]
public sealed class JobSeekerCvController
    : ControllerBase
{
    private readonly IJobSeekerCvService
        _jobSeekerCvService;

    public JobSeekerCvController(
        IJobSeekerCvService jobSeekerCvService)
    {
        _jobSeekerCvService =
            jobSeekerCvService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(JobSeekerCvDto),
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
    public async Task<ActionResult<JobSeekerCvDto>>
        GetOwnCv(
            CancellationToken cancellationToken)
    {
        var cv =
            await _jobSeekerCvService
                .GetOwnCvAsync(
                    cancellationToken);

        if (cv is null)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status404NotFound,
                title:
                    "CV not found",
                detail:
                    "No current CV is available for the authenticated Job Seeker.");
        }

        return Ok(
            cv);
    }

    [HttpPost]
    [Consumes(
        "multipart/form-data")]
    [ProducesResponseType(
        typeof(JobSeekerCvDto),
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
        StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<JobSeekerCvDto>>
        UploadOwnCv(
            [FromForm(
                Name = "file")]
            IFormFile? file,
            CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
        {
            return InvalidMultipartRequest();
        }

        IFormCollection form;

        try
        {
            form =
                await Request.ReadFormAsync(
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is InvalidDataException
                or IOException)
        {
            return InvalidMultipartRequest();
        }

        if (file is null ||
            form.Count != 0 ||
            form.Files.Count != 1 ||
            !string.Equals(
                form.Files[0].Name,
                "file",
                StringComparison.Ordinal))
        {
            return InvalidMultipartRequest();
        }

        var submittedFile =
            form.Files[0];

        if (submittedFile.Length <= 0)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title:
                    "Invalid CV file",
                detail:
                    "The CV file must not be empty.");
        }

        if (submittedFile.Length >
            CvValidationPolicy.MaxCvBytes)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status413PayloadTooLarge,
                title:
                    "CV file is too large",
                detail:
                    "The CV file must not exceed 5,000,000 bytes.");
        }

        CvUploadResult result;

        try
        {
            await using var content =
                submittedFile.OpenReadStream();

            result =
                await _jobSeekerCvService
                    .UploadOrReplaceOwnCvAsync(
                        new CvUploadRequest(
                            content,
                            submittedFile.FileName,
                            submittedFile.ContentType),
                        cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is IOException
                or InvalidOperationException)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status400BadRequest,
                title:
                    "Invalid CV upload",
                detail:
                    "The uploaded CV could not be read.");
        }

        if (result.Succeeded &&
            result.Cv is not null)
        {
            return Ok(
                result.Cv);
        }

        return result.FailureReason switch
        {
            CvOperationFailureReason
                    .InvalidAuthenticatedUser =>
                Problem(
                    statusCode:
                        StatusCodes.Status401Unauthorized,
                    title:
                        "Invalid authenticated user",
                    detail:
                        "The authenticated Job Seeker could not be resolved."),

            CvOperationFailureReason
                    .ProfileNotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "Job Seeker profile not found",
                    detail:
                        "A Job Seeker profile is required before uploading a CV."),

            CvOperationFailureReason
                    .InvalidInput or
            CvOperationFailureReason
                    .InvalidFileType =>
                Problem(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Invalid CV file",
                    detail:
                        "Only one valid PDF or DOCX CV file may be uploaded."),

            CvOperationFailureReason
                    .FileTooLarge =>
                Problem(
                    statusCode:
                        StatusCodes.Status413PayloadTooLarge,
                    title:
                        "CV file is too large",
                    detail:
                        "The CV file must not exceed 5,000,000 bytes."),

            CvOperationFailureReason
                    .MalformedFile =>
                Problem(
                    statusCode:
                        StatusCodes.Status415UnsupportedMediaType,
                    title:
                        "Malformed CV file",
                    detail:
                        "The uploaded CV content does not match an approved PDF or DOCX file."),

            CvOperationFailureReason
                    .StorageUnavailable =>
                Problem(
                    statusCode:
                        StatusCodes.Status503ServiceUnavailable,
                    title:
                        "CV storage unavailable",
                    detail:
                        "Protected CV storage is temporarily unavailable."),

            CvOperationFailureReason
                    .PersistenceFailed =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "CV update failed",
                    detail:
                        "The CV metadata could not be saved."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "CV update failed")
        };
    }

    [HttpGet(
        "file")]
    [ProducesResponseType(
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
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult>
        DownloadOwnCv(
            CancellationToken cancellationToken)
    {
        var result =
            await _jobSeekerCvService
                .DownloadOwnCvAsync(
                    cancellationToken);

        if (result.Succeeded &&
            result.Content is not null &&
            !string.IsNullOrWhiteSpace(
                result.OriginalFileName) &&
            !string.IsNullOrWhiteSpace(
                result.ContentType))
        {
            SetProtectedFileCacheHeaders();

            return File(
                result.Content,
                result.ContentType,
                result.OriginalFileName,
                enableRangeProcessing:
                    false);
        }

        return result.FailureReason switch
        {
            CvDownloadFailureReason
                    .InvalidAuthenticatedUser =>
                Problem(
                    statusCode:
                        StatusCodes.Status401Unauthorized,
                    title:
                        "Invalid authenticated user",
                    detail:
                        "The authenticated Job Seeker could not be resolved."),

            CvDownloadFailureReason
                    .NotFound =>
                Problem(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "CV not found",
                    detail:
                        "No current CV file is available for the authenticated Job Seeker."),

            CvDownloadFailureReason
                    .StorageUnavailable =>
                Problem(
                    statusCode:
                        StatusCodes.Status503ServiceUnavailable,
                    title:
                        "CV storage unavailable",
                    detail:
                        "The protected CV file is temporarily unavailable."),

            _ =>
                Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "CV download failed")
        };
    }

    private ObjectResult InvalidMultipartRequest()
    {
        return Problem(
            statusCode:
                StatusCodes.Status400BadRequest,
            title:
                "Invalid CV upload",
            detail:
                "Provide exactly one multipart file field named 'file' and no additional form fields.");
    }

    private void SetProtectedFileCacheHeaders()
    {
        Response.Headers[
            "Cache-Control"] =
            "private, no-store, no-cache";

        Response.Headers[
            "Pragma"] =
            "no-cache";

        Response.Headers[
            "Expires"] =
            "0";
    }
}
