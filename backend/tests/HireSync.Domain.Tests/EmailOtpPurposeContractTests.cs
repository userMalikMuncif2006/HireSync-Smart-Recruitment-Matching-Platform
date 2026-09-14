using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests;

public class EmailOtpPurposeContractTests
{
    [Fact]
    public void Email_otp_purposes_are_stable()
    {
        Assert.Equal(
            1,
            (byte)EmailOtpPurpose.EmployerRegistration);

        Assert.Equal(
            2,
            (byte)EmailOtpPurpose.AdministratorFirstActivation);
    }
}
