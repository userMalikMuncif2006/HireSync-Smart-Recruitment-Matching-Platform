using HireSync.Application.Security;

namespace HireSync.Application.Tests;

public class RoleNamesContractTests
{
    [Fact]
    public void HireSync_roles_are_frozen()
    {
        Assert.Equal("JobSeeker", RoleNames.JobSeeker);
        Assert.Equal("Employer", RoleNames.Employer);
        Assert.Equal("Administrator", RoleNames.Administrator);

        Assert.Equal(
            [RoleNames.JobSeeker, RoleNames.Employer, RoleNames.Administrator],
            RoleNames.All);
    }
}
