using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class JobSeekerSkillConfiguration
    : IEntityTypeConfiguration<JobSeekerSkill>
{
    public void Configure(EntityTypeBuilder<JobSeekerSkill> entity)
    {
        entity.ToTable("JobSeekerSkills");

        entity.HasKey(jobSeekerSkill => new
        {
            jobSeekerSkill.JobSeekerProfileId,
            jobSeekerSkill.SkillId
        });

        entity.HasOne<JobSeekerProfile>()
            .WithMany()
            .HasForeignKey(jobSeekerSkill => jobSeekerSkill.JobSeekerProfileId);

        entity.HasOne<Skill>()
            .WithMany()
            .HasForeignKey(jobSeekerSkill => jobSeekerSkill.SkillId);
    }
}
