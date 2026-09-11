using HireSync.Domain.Entities;
using HireSync.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class JobSeekerProfileConfiguration
    : IEntityTypeConfiguration<JobSeekerProfile>
{
    public void Configure(EntityTypeBuilder<JobSeekerProfile> entity)
    {
        entity.ToTable("JobSeekerProfiles");

        entity.HasKey(profile => profile.Id);

        entity.Property(profile => profile.TotalExperienceMonths)
            .IsRequired(false);

        entity.Property(profile => profile.EducationLevel)
            .IsRequired(false);

        entity.Property(profile => profile.PreferredLocation)
            .HasMaxLength(100)
            .IsRequired(false);

        entity.Property(profile => profile.NormalizedPreferredLocation)
            .HasMaxLength(100)
            .IsRequired(false);

        entity.Property(profile => profile.CreatedAtUtc)
            .HasPrecision(3);

        entity.Property(profile => profile.UpdatedAtUtc)
            .HasPrecision(3);

        entity.HasIndex(profile => profile.UserId)
            .IsUnique();

        entity.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<JobSeekerProfile>(profile => profile.UserId);
    }
}
