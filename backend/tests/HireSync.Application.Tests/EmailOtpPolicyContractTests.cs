using HireSync.Application.Security;

namespace HireSync.Application.Tests;

public class EmailOtpPolicyContractTests
{
    [Fact]
    public void Email_otp_policy_values_are_stable()
    {
        Assert.Equal(6, EmailOtpPolicy.CodeLength);
        Assert.Equal(
            TimeSpan.FromMinutes(5),
            EmailOtpPolicy.Lifetime);
        Assert.Equal(5, EmailOtpPolicy.MaxFailedAttempts);
        Assert.Equal(
            TimeSpan.FromSeconds(60),
            EmailOtpPolicy.ResendCooldown);
    }
}
