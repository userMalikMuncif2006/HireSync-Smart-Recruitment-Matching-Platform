namespace HireSync.Domain.Entities;

public sealed class JobSeekerSkill
{
    public Guid JobSeekerProfileId { get; private set; }

    public Guid SkillId { get; private set; }

    private JobSeekerSkill()
    {
    }

    public JobSeekerSkill(
        Guid jobSeekerProfileId,
        Guid skillId)
    {
        if (jobSeekerProfileId == Guid.Empty)
        {
            throw new ArgumentException(
                "Job Seeker profile ID cannot be empty.",
                nameof(jobSeekerProfileId));
        }

        if (skillId == Guid.Empty)
        {
            throw new ArgumentException(
                "Skill ID cannot be empty.",
                nameof(skillId));
        }

        JobSeekerProfileId = jobSeekerProfileId;
        SkillId = skillId;
    }
}
