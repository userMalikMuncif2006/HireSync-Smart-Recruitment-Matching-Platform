using HireSync.Domain.Entities;
using HireSync.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class EmployerProfileConfiguration
    : IEntityTypeConfiguration<EmployerProfile>
{
    public void Configure(
        EntityTypeBuilder<EmployerProfile> builder)
    {
        builder.ToTable("EmployerProfiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.CompanyName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(profile => profile.NormalizedCompanyName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(profile => profile.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(profile => profile.Location)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(profile => profile.NormalizedLocation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(profile => profile.ContactPersonName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(profile => profile.ContactPersonDesignation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(profile => profile.BusinessRegistrationNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(
                profile =>
                    profile.NormalizedBusinessRegistrationNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(profile => profile.MobileNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(profile => profile.CompanyWebsite)
            .HasMaxLength(300);

        builder.Property(profile => profile.CreatedAtUtc)
            .HasPrecision(3);

        builder.Property(profile => profile.UpdatedAtUtc)
            .HasPrecision(3);

        builder.HasIndex(profile => profile.UserId)
            .IsUnique();

        builder.HasIndex(
                profile =>
                    profile.NormalizedBusinessRegistrationNumber)
            .IsUnique();

        builder.HasIndex(profile => profile.NormalizedCompanyName);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<EmployerProfile>(
                profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}