using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Rules;

namespace HireSync.Application.Tests.Rules;

public class VacancySearchRulesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("developer")]
    public void IsValidQuery_ReturnsTrue_ForValidValues(string? query)
    {
        Assert.True(VacancySearchRules.IsValidQuery(query));
    }

    [Fact]
    public void IsValidQuery_ReturnsFalse_WhenTooLong()
    {
        var query = new string(
            'A',
            VacancySearchRules.QueryMaxLength + 1);

        Assert.False(
            VacancySearchRules.IsValidQuery(query));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Colombo")]
    [InlineData("Kandy")]
    public void IsValidLocation_ReturnsTrue_ForValidValues(
        string? location)
    {
        Assert.True(
            VacancySearchRules.IsValidLocation(location));
    }

    [Fact]
    public void IsValidLocation_ReturnsFalse_WhenTooLong()
    {
        var location = new string(
            'A',
            VacancySearchRules.LocationMaxLength + 1);

        Assert.False(
            VacancySearchRules.IsValidLocation(location));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    public void IsValidPage_ReturnsTrue_ForPositiveValues(int page)
    {
        Assert.True(
            VacancySearchRules.IsValidPage(page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsValidPage_ReturnsFalse_ForInvalidValues(int page)
    {
        Assert.False(
            VacancySearchRules.IsValidPage(page));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(50)]
    public void IsValidPageSize_ReturnsTrue_InRange(int pageSize)
    {
        Assert.True(
            VacancySearchRules.IsValidPageSize(pageSize));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [InlineData(-1)]
    public void IsValidPageSize_ReturnsFalse_OutOfRange(int pageSize)
    {
        Assert.False(
            VacancySearchRules.IsValidPageSize(pageSize));
    }

    [Fact]
    public void IsValidSort_ReturnsTrue_ForNewest()
    {
        Assert.True(
            VacancySearchRules.IsValidSort(
                VacancySearchSort.Newest));
    }

    [Fact]
    public void IsValidSort_ReturnsTrue_ForMatch()
    {
        Assert.True(
            VacancySearchRules.IsValidSort(
                VacancySearchSort.Match));
    }

    [Fact]
    public void IsValidSort_ReturnsFalse_ForUnknownValue()
    {
        var unknown = (VacancySearchSort)99;

        Assert.False(
            VacancySearchRules.IsValidSort(unknown));
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForValidRequest()
    {
        var request = new SearchVacanciesRequest(
            Q: "developer",
            Location: "Colombo",
            Sort: VacancySearchSort.Newest,
            Page: 1,
            PageSize: 20);

        Assert.True(
            VacancySearchRules.IsValid(request));
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForInvalidRequest()
    {
        var request = new SearchVacanciesRequest(
            Q: "developer",
            Location: "Colombo",
            Sort: VacancySearchSort.Newest,
            Page: 0,
            PageSize: 20);

        Assert.False(
            VacancySearchRules.IsValid(request));
    }
}