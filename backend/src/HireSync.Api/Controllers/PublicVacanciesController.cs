using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Vacancy;
using HireSync.Application.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.Controllers;

[ApiController]
[Route("api/v1/vacancies")]
[AllowAnonymous]
public sealed class PublicVacanciesController : ControllerBase
{
    private readonly IVacancySearchService _vacancySearchService;

    public PublicVacanciesController(
        IVacancySearchService vacancySearchService)
    {
        _vacancySearchService = vacancySearchService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PublicVacancyPageDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublicVacancyPageDto>>
        SearchVacancies(
            [FromQuery] SearchVacanciesRequest request,
            CancellationToken cancellationToken)
    {
        if (!VacancySearchRules.IsValid(request))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid vacancy search",
                detail: "The submitted vacancy search parameters are invalid.");
        }

        if (!VacancySearchRules.IsBasicSearchSortSupported(
                request.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Unsupported vacancy sort",
                detail: "Match sorting is not available until the matching workflow is integrated.");
        }

        var result =
            await _vacancySearchService.SearchOpenVacanciesAsync(
                request,
                cancellationToken);

        return Ok(result);
    }
}