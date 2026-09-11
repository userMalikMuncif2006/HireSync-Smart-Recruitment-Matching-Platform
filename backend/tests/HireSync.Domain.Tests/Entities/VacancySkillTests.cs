using HireSync.Domain.Entities;

namespace HireSync.Domain.Tests.Entities;

public sealed class VacancySkillTests
{
    [Fact]
    public void Constructor_WithValidIds_CreatesAssociation()
    {
        var vacancyId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var association = new VacancySkill(
            vacancyId,
            skillId);

        Assert.Equal(vacancyId, association.VacancyId);
        Assert.Equal(skillId, association.SkillId);
    }

    [Fact]
    public void Constructor_WithEmptyVacancyId_ThrowsArgumentException()
    {
        var skillId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new VacancySkill(
                Guid.Empty,
                skillId));
    }

    [Fact]
    public void Constructor_WithEmptySkillId_ThrowsArgumentException()
    {
        var vacancyId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new VacancySkill(
                vacancyId,
                Guid.Empty));
    }

    [Fact]
    public void Constructor_PreservesCanonicalIdsUnchanged()
    {
        var vacancyId =
            Guid.Parse("33333333-3333-3333-3333-333333333333");

        var skillId =
            Guid.Parse("44444444-4444-4444-4444-444444444444");

        var association = new VacancySkill(
            vacancyId,
            skillId);

        Assert.Equal(vacancyId, association.VacancyId);
        Assert.Equal(skillId, association.SkillId);
    }
}