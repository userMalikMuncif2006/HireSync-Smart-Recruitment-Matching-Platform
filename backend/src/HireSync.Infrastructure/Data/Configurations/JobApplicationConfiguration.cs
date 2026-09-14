using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class JobApplicationConfiguration
    : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> entity)
    {
        entity.ToTable("JobApplications");

        entity.HasKey(application => application.Id);

        entity.Property(application => application.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        entity.Property(application => application.AppliedAtUtc)
            .HasPrecision(3);

        entity.Property(application => application.UpdatedAtUtc)
            .HasPrecision(3);

        entity.Property(application => application.RowVersion)
            .IsRowVersion();

        entity.HasIndex(application => new
        {
            application.VacancyId,
            application.JobSeekerProfileId
        })
        .IsUnique();

        entity.HasIndex(application => new
        {
            application.VacancyId,
            application.Status,
            application.AppliedAtUtc
        });

        entity.HasIndex(application => new
        {
            application.JobSeekerProfileId,
            application.AppliedAtUtc
        });

        entity.HasOne<Vacancy>()
            .WithMany()
            .HasForeignKey(application => application.VacancyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<JobSeekerProfile>()
            .WithMany()
            .HasForeignKey(application => application.JobSeekerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
