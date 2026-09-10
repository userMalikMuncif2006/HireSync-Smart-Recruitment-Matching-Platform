using HireSync.Domain.Entities;

namespace HireSync.Domain.Tests.Entities;

public sealed class JobSeekerSkillTests
{
    [Fact]
    public void Constructor_WithValidIds_CreatesAssociation()
    {
        var profileId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var association = new JobSeekerSkill(
            profileId,
            skillId);

        Assert.Equal(profileId, association.JobSeekerProfileId);
        Assert.Equal(skillId, association.SkillId);
    }

    [Fact]
    public void Constructor_WithEmptyJobSeekerProfileId_ThrowsArgumentException()
    {
        var skillId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new JobSeekerSkill(
                Guid.Empty,
                skillId));
    }

    [Fact]
    public void Constructor_WithEmptySkillId_ThrowsArgumentException()
    {
        var profileId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new JobSeekerSkill(
                profileId,
                Guid.Empty));
    }

    [Fact]
    public void Constructor_PreservesCanonicalIdsUnchanged()
    {
        var profileId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        var skillId =
            Guid.Parse("22222222-2222-2222-2222-222222222222");

        var association = new JobSeekerSkill(
            profileId,
            skillId);

        Assert.Equal(profileId, association.JobSeekerProfileId);
        Assert.Equal(skillId, association.SkillId);
    }
}
