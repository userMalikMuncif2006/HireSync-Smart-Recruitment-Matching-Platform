using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Vacancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

public sealed class PublicVacanciesControllerContractTests
{
    [Fact]
    public void Controller_allows_anonymous_access()
    {
        var attribute =
            typeof(PublicVacanciesController)
                .GetCustomAttributes(
                    typeof(AllowAnonymousAttribute),
                    inherit: true)
                .Cast<AllowAnonymousAttribute>()
                .Single();

        Assert.NotNull(attribute);
    }

    [Fact]
    public void Controller_uses_public_vacancy_route()
    {
        var attribute =
            typeof(PublicVacanciesController)
                .GetCustomAttributes(
                    typeof(RouteAttribute),
                    inherit: true)
                .Cast<RouteAttribute>()
                .Single();

        Assert.Equal(
            "api/v1/vacancies",
            attribute.Template);
    }

    [Fact]
    public async Task SearchVacancies_returns_200_for_valid_newest_search()
    {
        var page =
            new PublicVacancyPageDto(
                Array.Empty<PublicVacancyListItemDto>(),
                Page: 1,
                PageSize: 20,
                TotalCount: 0);

        var service =
            new FakeVacancySearchService
            {
                Result = page
            };

        var controller =
            new PublicVacanciesController(service);

        var request =
            new SearchVacanciesRequest(
                Q: "developer",
                Location: "Colombo",
                Sort: VacancySearchSort.Newest,
                Page: 1,
                PageSize: 20);

        var result =
            await controller.SearchVacancies(
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<PublicVacancyPageDto>(
                ok.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            ok.StatusCode);

        Assert.Same(
            page,
            response);

        Assert.Equal(
            1,
            service.CallCount);

        Assert.Same(
            request,
            service.LastRequest);
    }

    [Fact]
    public async Task SearchVacancies_returns_400_for_invalid_request()
    {
        var service =
            new FakeVacancySearchService();

        var controller =
            new PublicVacanciesController(service);

        var request =
            new SearchVacanciesRequest(
                Page: 0,
                PageSize: 20);

        var result =
            await controller.SearchVacancies(
                request,
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

        Assert.Null(
            service.LastRequest);
    }

    [Fact]
    public async Task SearchVacancies_returns_400_for_match_sort_until_matching_is_integrated()
    {
        var service =
            new FakeVacancySearchService();

        var controller =
            new PublicVacanciesController(service);

        var request =
            new SearchVacanciesRequest(
                Sort: VacancySearchSort.Match);

        var result =
            await controller.SearchVacancies(
                request,
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

        Assert.Null(
            service.LastRequest);
    }

    private sealed class FakeVacancySearchService
        : IVacancySearchService
    {
        public PublicVacancyPageDto Result { get; set; } =
            new(
                Array.Empty<PublicVacancyListItemDto>(),
                Page: 1,
                PageSize: 20,
                TotalCount: 0);

        public int CallCount { get; private set; }

        public SearchVacanciesRequest? LastRequest { get; private set; }

        public Task<PublicVacancyPageDto>
            SearchOpenVacanciesAsync(
                SearchVacanciesRequest request,
                CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;

            return Task.FromResult(Result);
        }
    }
}