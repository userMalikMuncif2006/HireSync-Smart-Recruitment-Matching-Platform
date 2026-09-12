using HireSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireSync.Infrastructure.Data.Configurations;

public sealed class CvDocumentConfiguration
    : IEntityTypeConfiguration<CvDocument>
{
    public void Configure(
        EntityTypeBuilder<CvDocument> entity)
    {
        entity.ToTable(
            "CvDocuments",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_CvDocuments_SizeBytes",
                    "[SizeBytes] >= 1 AND [SizeBytes] <= 5000000");

                table.HasCheckConstraint(
                    "CK_CvDocuments_Extension",
                    "[Extension] IN ('.pdf', '.docx')");
            });

        entity.HasKey(
            document =>
                document.Id);

        entity.Property(
                document =>
                    document.OriginalFileName)
            .HasMaxLength(
                CvDocument.MaxOriginalFileNameLength)
            .IsRequired();

        entity.Property(
                document =>
                    document.StoredFileName)
            .HasMaxLength(
                CvDocument.MaxStoredFileNameLength)
            .IsRequired();

        entity.Property(
                document =>
                    document.RelativeStoragePath)
            .HasMaxLength(
                CvDocument.MaxRelativeStoragePathLength)
            .IsRequired();

        entity.Property(
                document =>
                    document.Extension)
            .HasMaxLength(
                CvDocument.MaxExtensionLength)
            .IsRequired();

        entity.Property(
                document =>
                    document.ContentType)
            .HasMaxLength(
                CvDocument.MaxContentTypeLength)
            .IsRequired();

        entity.Property(
                document =>
                    document.SizeBytes)
            .IsRequired();

        entity.Property(
                document =>
                    document.Sha256Hash)
            .HasColumnType("char(64)")
            .IsRequired();

        entity.Property(
                document =>
                    document.UploadedAtUtc)
            .HasPrecision(3);

        entity.HasIndex(
                document =>
                    document.JobSeekerProfileId)
            .IsUnique();

        entity.HasIndex(
                document =>
                    document.StoredFileName)
            .IsUnique();

        entity.HasOne<JobSeekerProfile>()
            .WithOne()
            .HasForeignKey<CvDocument>(
                document =>
                    document.JobSeekerProfileId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}
