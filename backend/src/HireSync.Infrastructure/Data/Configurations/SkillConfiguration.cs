using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class SkillConfiguration
    : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> entity)
    {
        entity.ToTable("Skills");

        entity.HasKey(skill => skill.Id);

        entity.Property(skill => skill.Name)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(skill => skill.NormalizedName)
            .HasMaxLength(50)
            .IsRequired();

        entity.HasIndex(skill => skill.NormalizedName)
            .IsUnique();
    }
}
