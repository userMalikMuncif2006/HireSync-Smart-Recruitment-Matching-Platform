using HireSync.Application.DTOs.Vacancy;
using HireSync.Application.Rules;
using HireSync.Domain.Enums;

namespace HireSync.Application.Tests.Rules;

public class VacancyStatusUpdateRulesTests
{
    [Fact]
    public void IsRequestedStatusValid_ReturnsTrue_ForClosed()
    {
        Assert.True(
            VacancyStatusUpdateRules.IsRequestedStatusValid(
                VacancyStatus.Closed));
    }

    [Fact]
    public void IsRequestedStatusValid_ReturnsFalse_ForOpen()
    {
        Assert.False(
            VacancyStatusUpdateRules.IsRequestedStatusValid(
                VacancyStatus.Open));
    }

    [Fact]
    public void IsRequestedStatusValid_ReturnsFalse_ForUnknownStatus()
    {
        var unknownStatus = (VacancyStatus)99;

        Assert.False(
            VacancyStatusUpdateRules.IsRequestedStatusValid(
                unknownStatus));
    }

    [Fact]
    public void HasValidRowVersion_ReturnsTrue_ForNonEmptyValue()
    {
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        Assert.True(
            VacancyStatusUpdateRules.HasValidRowVersion(
                rowVersion));
    }

    [Fact]
    public void HasValidRowVersion_ReturnsFalse_ForEmptyValue()
    {
        Assert.False(
            VacancyStatusUpdateRules.HasValidRowVersion(
                Array.Empty<byte>()));
    }

    [Fact]
    public void HasValidRowVersion_ReturnsFalse_ForNull()
    {
        Assert.False(
            VacancyStatusUpdateRules.HasValidRowVersion(
                null));
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForClosedWithRowVersion()
    {
        var request = new UpdateVacancyStatusRequest(
            Status: VacancyStatus.Closed,
            RowVersion: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        Assert.True(
            VacancyStatusUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForOpenStatus()
    {
        var request = new UpdateVacancyStatusRequest(
            Status: VacancyStatus.Open,
            RowVersion: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        Assert.False(
            VacancyStatusUpdateRules.IsValid(request));
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForMissingRowVersion()
    {
        var request = new UpdateVacancyStatusRequest(
            Status: VacancyStatus.Closed,
            RowVersion: Array.Empty<byte>());

        Assert.False(
            VacancyStatusUpdateRules.IsValid(request));
    }
}