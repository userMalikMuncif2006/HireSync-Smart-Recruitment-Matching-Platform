using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class ContactRequestConfiguration
    : IEntityTypeConfiguration<ContactRequest>
{
    public void Configure(EntityTypeBuilder<ContactRequest> entity)
    {
        entity.ToTable("ContactRequests");

        entity.HasKey(request => request.Id);

        entity.Property(request => request.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        entity.Property(request => request.RequestedAtUtc)
            .HasPrecision(3);

        entity.Property(request => request.RespondedAtUtc)
            .HasPrecision(3)
            .IsRequired(false);

        entity.Property(request => request.RowVersion)
            .IsRowVersion();

        entity.HasIndex(request => request.JobApplicationId)
            .IsUnique();

        entity.HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(request => request.JobApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
