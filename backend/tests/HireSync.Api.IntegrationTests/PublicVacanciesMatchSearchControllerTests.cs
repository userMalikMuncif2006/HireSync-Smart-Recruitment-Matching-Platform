using HireSync.Api.Controllers;
using HireSync.Application.DTOs.Matching;
using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Interfaces.Matching;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Vacancy;
using HireSync.Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.Api.IntegrationTests;

internal sealed class NoOpVacancyMatchSearchService
    : IVacancyMatchSearchService
{
    public Task<VacancySearchQueryResult>
        SearchOpenVacanciesByMatchAsync(
            Guid jobSeekerUserId,
            SearchVacanciesRequest request,
            CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            VacancySearchQueryResult.Success(
                new PublicVacancyPageDto(
                    Array.Empty<PublicVacancyListItemDto>(),
                    request.Page,
                    request.PageSize,
                    0)));
    }
}

public sealed class PublicVacanciesMatchSearchControllerTests
{
    [Fact]
    public async Task Match_sort_uses_match_service()
    {
        var userId = Guid.NewGuid();

        var expected =
            new PublicVacancyPageDto(
                Array.Empty<PublicVacancyListItemDto>(),
                1,
                20,
                0);

        var basic =
            new TrackingBasicSearchService(expected);

        var match =
            new TrackingMatchSearchService(
                VacancySearchQueryResult.Success(
                    expected));

        var controller =
            CreateController(
                basic,
                match,
                new FakeCurrentUser(
                    true,
                    userId));

        var request =
            new SearchVacanciesRequest(
                Sort: VacancySearchSort.Match);

        var action =
            await controller.SearchVacancies(
                request,
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                action.Result);

        Assert.Same(
            expected,
            ok.Value);

        Assert.Equal(
            0,
            basic.CallCount);

        Assert.Equal(
            1,
            match.CallCount);

        Assert.Equal(
            userId,
            match.LastUserId);

        Assert.Same(
            request,
            match.LastRequest);
    }

    [Fact]
    public async Task Newest_sort_keeps_existing_basic_service_path()
    {
        var expected =
            new PublicVacancyPageDto(
                Array.Empty<PublicVacancyListItemDto>(),
                1,
                20,
                0);

        var basic =
            new TrackingBasicSearchService(
                expected);

        var match =
            new TrackingMatchSearchService(
                VacancySearchQueryResult.Failure(
                    VacancySearchFailureReason.InvalidInput));

        var controller =
            CreateController(
                basic,
                match,
                new FakeCurrentUser(
                    true,
                    Guid.NewGuid()));

        var action =
            await controller.SearchVacancies(
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Newest),
                CancellationToken.None);

        var ok =
            Assert.IsType<OkObjectResult>(
                action.Result);

        Assert.Same(
            expected,
            ok.Value);

        Assert.Equal(
            1,
            basic.CallCount);

        Assert.Equal(
            0,
            match.CallCount);
    }

    [Fact]
    public async Task Match_sort_returns_401_for_invalid_current_user()
    {
        var match =
            new TrackingMatchSearchService(
                VacancySearchQueryResult.Failure(
                    VacancySearchFailureReason.InvalidInput));

        var controller =
            CreateController(
                new TrackingBasicSearchService(
                    EmptyPage()),
                match,
                new FakeCurrentUser(
                    false,
                    null));

        var action =
            await controller.SearchVacancies(
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Match),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            problem.StatusCode);

        Assert.Equal(
            0,
            match.CallCount);
    }

    [Theory]
    [InlineData(
        VacancySearchFailureReason.InvalidInput,
        StatusCodes.Status400BadRequest)]
    [InlineData(
        VacancySearchFailureReason.JobSeekerUnavailable,
        StatusCodes.Status403Forbidden)]
    [InlineData(
        VacancySearchFailureReason.ProfileNotReady,
        StatusCodes.Status400BadRequest)]
    [InlineData(
        VacancySearchFailureReason.InvalidMatchingData,
        StatusCodes.Status500InternalServerError)]
    public async Task Match_sort_maps_failures(
        VacancySearchFailureReason reason,
        int expectedStatus)
    {
        var controller =
            CreateController(
                new TrackingBasicSearchService(
                    EmptyPage()),
                new TrackingMatchSearchService(
                    VacancySearchQueryResult.Failure(
                        reason)),
                new FakeCurrentUser(
                    true,
                    Guid.NewGuid()));

        var action =
            await controller.SearchVacancies(
                new SearchVacanciesRequest(
                    Sort: VacancySearchSort.Match),
                CancellationToken.None);

        var problem =
            Assert.IsType<ObjectResult>(
                action.Result);

        Assert.Equal(
            expectedStatus,
            problem.StatusCode);
    }

    private static PublicVacanciesController
        CreateController(
            IVacancySearchService basic,
            IVacancyMatchSearchService match,
            ICurrentUser currentUser)
    {
        return new PublicVacanciesController(
            basic,
            match,
            new NoOpVacancyDetailService(),
            currentUser);
    }

    private static PublicVacancyPageDto
        EmptyPage()
    {
        return new PublicVacancyPageDto(
            Array.Empty<PublicVacancyListItemDto>(),
            1,
            20,
            0);
    }

    private sealed class TrackingBasicSearchService
        : IVacancySearchService
    {
        private readonly PublicVacancyPageDto
            _result;

        public TrackingBasicSearchService(
            PublicVacancyPageDto result)
        {
            _result = result;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Task<PublicVacancyPageDto>
            SearchOpenVacanciesAsync(
                SearchVacanciesRequest request,
                CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                _result);
        }
    }

    private sealed class TrackingMatchSearchService
        : IVacancyMatchSearchService
    {
        private readonly VacancySearchQueryResult
            _result;

        public TrackingMatchSearchService(
            VacancySearchQueryResult result)
        {
            _result = result;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public Guid? LastUserId
        {
            get;
            private set;
        }

        public SearchVacanciesRequest?
            LastRequest
        {
            get;
            private set;
        }

        public Task<VacancySearchQueryResult>
            SearchOpenVacanciesByMatchAsync(
                Guid jobSeekerUserId,
                SearchVacanciesRequest request,
                CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastUserId = jobSeekerUserId;
            LastRequest = request;

            return Task.FromResult(
                _result);
        }
    }

    private sealed class NoOpVacancyDetailService
        : IVacancyDetailService
    {
        public Task<VacancyDetailQueryResult>
            GetDetailAsync(
                Guid jobSeekerUserId,
                Guid vacancyId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                VacancyDetailQueryResult.Failure(
                    VacancyDetailFailureReason.InvalidInput));
        }
    }

    private sealed class FakeCurrentUser
        : ICurrentUser
    {
        public FakeCurrentUser(
            bool isAuthenticated,
            Guid? userId)
        {
            IsAuthenticated =
                isAuthenticated;

            UserId =
                userId;
        }

        public bool IsAuthenticated
        {
            get;
        }

        public Guid? UserId
        {
            get;
        }

        public string? Role =>
            RoleNames.JobSeeker;

        public string? Email =>
            "jobseeker@example.com";
    }
}