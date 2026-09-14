using HireSync.Domain.Enums;

namespace HireSync.Domain.Tests.Enums;

public sealed class EducationLevelTests
{
    [Fact]
    public void EducationLevel_UsesFrozenMatchingRanks()
    {
        Assert.Equal(0, (int)EducationLevel.NoFormalQualification);
        Assert.Equal(1, (int)EducationLevel.OrdinaryLevel);
        Assert.Equal(2, (int)EducationLevel.AdvancedLevel);
        Assert.Equal(3, (int)EducationLevel.Certificate);
        Assert.Equal(4, (int)EducationLevel.Diploma);
        Assert.Equal(5, (int)EducationLevel.Bachelor);
        Assert.Equal(6, (int)EducationLevel.PostgraduateDiploma);
        Assert.Equal(7, (int)EducationLevel.Master);
        Assert.Equal(8, (int)EducationLevel.Doctorate);
    }
}