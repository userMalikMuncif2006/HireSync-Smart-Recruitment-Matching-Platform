using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Rules;
using HireSync.Domain.Enums;

namespace HireSync.Application.Tests.Rules;

public sealed class EmployerVacancyListRulesTests
{
    [Fact]
    public void IsValid_returns_true_for_default_request()
    {
        var request =
            new EmployerVacancyListRequest();

        Assert.True(
            EmployerVacancyListRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_true_for_open_status_filter()
    {
        var request =
            new EmployerVacancyListRequest(
                Status: VacancyStatus.Open,
                Page: 1,
                PageSize: 20);

        Assert.True(
            EmployerVacancyListRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_true_for_closed_status_filter()
    {
        var request =
            new EmployerVacancyListRequest(
                Status: VacancyStatus.Closed,
                Page: 2,
                PageSize: 50);

        Assert.True(
            EmployerVacancyListRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_unknown_status()
    {
        var request =
            new EmployerVacancyListRequest(
                Status: (VacancyStatus)99,
                Page: 1,
                PageSize: 20);

        Assert.False(
            EmployerVacancyListRules.IsValid(request));
    }

    [Fact]
    public void IsValid_returns_false_for_invalid_page()
    {
        var request =
            new EmployerVacancyListRequest(
                Page: 0,
                PageSize: 20);

        Assert.False(
            EmployerVacancyListRules.IsValid(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [InlineData(-1)]
    public void IsValid_returns_false_for_invalid_page_size(
        int pageSize)
    {
        var request =
            new EmployerVacancyListRequest(
                Page: 1,
                PageSize: pageSize);

        Assert.False(
            EmployerVacancyListRules.IsValid(request));
    }
}