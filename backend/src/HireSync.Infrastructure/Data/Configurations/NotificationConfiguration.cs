using HireSync.Domain.Entities;
using HireSync.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> entity)
    {
        entity.ToTable("Notifications");

        entity.HasKey(notification => notification.Id);

        entity.Property(notification => notification.Type)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        entity.Property(notification => notification.Title)
            .HasMaxLength(Notification.TitleMaxLength)
            .IsRequired();

        entity.Property(notification => notification.Message)
            .HasMaxLength(Notification.MessageMaxLength)
            .IsRequired();

        entity.Property(notification => notification.IsRead)
            .HasDefaultValue(false)
            .IsRequired();

        entity.Property(notification => notification.CreatedAtUtc)
            .HasPrecision(3);

        entity.Property(notification => notification.ReadAtUtc)
            .HasPrecision(3)
            .IsRequired(false);

        entity.HasIndex(notification => new
        {
            notification.RecipientUserId,
            notification.IsRead,
            notification.CreatedAtUtc
        });

        entity.HasIndex(notification => new
        {
            notification.JobApplicationId,
            notification.CreatedAtUtc
        });

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(notification => notification.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<JobApplication>()
            .WithMany()
            .HasForeignKey(notification => notification.JobApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
