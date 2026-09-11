using HireSync.Domain.Entities;

namespace HireSync.Domain.Tests.Entities;

public sealed class EmployerProfileTests
{
    [Fact]
    public void IsProfileComplete_ReturnsTrue_WhenRequiredFieldsExist()
    {
        var profile = CreateCompleteProfile();

        Assert.True(profile.IsProfileComplete);
    }

    [Fact]
    public void IsProfileComplete_ReturnsFalse_WhenRequiredFieldMissing()
    {
        var profile = CreateCompleteProfile();
        profile.BusinessRegistrationNumber = "   ";

        Assert.False(profile.IsProfileComplete);
    }

    [Fact]
    public void IsProfileComplete_DoesNotRequireCompanyWebsite()
    {
        var profile = CreateCompleteProfile();
        profile.CompanyWebsite = null;

        Assert.True(profile.IsProfileComplete);
    }

    [Fact]
    public void IsProfileComplete_DoesNotDependOnNormalizedHelpers()
    {
        var profile = CreateCompleteProfile();

        profile.NormalizedCompanyName = string.Empty;
        profile.NormalizedLocation = string.Empty;
        profile.NormalizedBusinessRegistrationNumber = string.Empty;

        Assert.True(profile.IsProfileComplete);
    }

    private static EmployerProfile CreateCompleteProfile()
    {
        return new EmployerProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompanyName = "HireSync Labs",
            Description = "A software company providing recruitment services.",
            Location = "Colombo",
            ContactPersonName = "Rasadh",
            ContactPersonDesignation = "Manager",
            BusinessRegistrationNumber = "BR-12345",
            MobileNumber = "+94771234567"
        };
    }
}