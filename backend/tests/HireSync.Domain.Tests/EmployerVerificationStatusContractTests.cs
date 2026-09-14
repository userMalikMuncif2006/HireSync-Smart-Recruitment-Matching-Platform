using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests;

public class EmployerVerificationStatusContractTests
{
    [Fact]
    public void Employer_verification_status_values_are_stable()
    {
        Assert.Equal(1, (byte)EmployerVerificationStatus.Pending);
        Assert.Equal(2, (byte)EmployerVerificationStatus.Approved);
        Assert.Equal(3, (byte)EmployerVerificationStatus.Rejected);
    }
}
