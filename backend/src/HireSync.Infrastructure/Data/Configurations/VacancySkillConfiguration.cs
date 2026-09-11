using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class VacancySkillConfiguration
    : IEntityTypeConfiguration<VacancySkill>
{
    public void Configure(EntityTypeBuilder<VacancySkill> entity)
    {
        entity.ToTable("VacancySkills");

        entity.HasKey(vacancySkill => new
        {
            vacancySkill.VacancyId,
            vacancySkill.SkillId
        });

        entity.HasOne<Vacancy>()
            .WithMany()
            .HasForeignKey(vacancySkill => vacancySkill.VacancyId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<Skill>()
            .WithMany()
            .HasForeignKey(vacancySkill => vacancySkill.SkillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}