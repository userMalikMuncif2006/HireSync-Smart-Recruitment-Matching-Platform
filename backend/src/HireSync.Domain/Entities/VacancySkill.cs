namespace HireSync.Domain.Entities;

public sealed class VacancySkill
{
    public Guid VacancyId { get; private set; }

    public Guid SkillId { get; private set; }

    private VacancySkill()
    {
    }

    public VacancySkill(
        Guid vacancyId,
        Guid skillId)
    {
        if (vacancyId == Guid.Empty)
        {
            throw new ArgumentException(
                "Vacancy ID cannot be empty.",
                nameof(vacancyId));
        }

        if (skillId == Guid.Empty)
        {
            throw new ArgumentException(
                "Skill ID cannot be empty.",
                nameof(skillId));
        }

        VacancyId = vacancyId;
        SkillId = skillId;
    }
}