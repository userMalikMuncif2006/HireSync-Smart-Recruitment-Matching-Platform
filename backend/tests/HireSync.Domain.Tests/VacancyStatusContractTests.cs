using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests;

public class VacancyStatusContractTests
{
    [Fact]
    public void VacancyStatus_values_are_frozen()
    {
        Assert.Equal((byte)1, (byte)VacancyStatus.Open);
        Assert.Equal((byte)2, (byte)VacancyStatus.Closed);
    }
}