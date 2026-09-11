using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class VacancyConfiguration
    : IEntityTypeConfiguration<Vacancy>
{
    public void Configure(EntityTypeBuilder<Vacancy> entity)
    {
        entity.ToTable("Vacancies");

        entity.HasKey(vacancy => vacancy.Id);

        entity.Property(vacancy => vacancy.Title)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(vacancy => vacancy.Description)
            .HasMaxLength(5000)
            .IsRequired();

        entity.Property(vacancy => vacancy.Location)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(vacancy => vacancy.NormalizedLocation)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(vacancy => vacancy.MinimumExperienceMonths)
            .IsRequired();

        entity.Property(vacancy => vacancy.RequiredEducationLevel)
            .HasConversion<byte?>()
            .HasColumnType("tinyint")
            .IsRequired(false);

        entity.Property(vacancy => vacancy.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        entity.Property(vacancy => vacancy.PublishedAtUtc)
            .HasPrecision(3);

        entity.Property(vacancy => vacancy.UpdatedAtUtc)
            .HasPrecision(3);

        entity.Property(vacancy => vacancy.ClosedAtUtc)
            .HasPrecision(3)
            .IsRequired(false);

        entity.Property(vacancy => vacancy.RowVersion)
            .IsRowVersion();

        entity.HasIndex(vacancy => vacancy.EmployerProfileId);

        entity.HasIndex(vacancy => vacancy.NormalizedLocation);

        entity.HasIndex(vacancy => vacancy.Status);

        entity.HasOne<EmployerProfile>()
            .WithMany()
            .HasForeignKey(vacancy => vacancy.EmployerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}