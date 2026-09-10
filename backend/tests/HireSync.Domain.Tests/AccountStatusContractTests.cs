using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests;

public class AccountStatusContractTests
{
    [Fact]
    public void AccountStatus_values_are_frozen()
    {
        Assert.Equal((byte)1, (byte)AccountStatus.Active);
        Assert.Equal((byte)2, (byte)AccountStatus.Suspended);
    }
}
