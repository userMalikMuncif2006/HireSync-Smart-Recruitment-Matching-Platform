using HireSync.Domain.Entities;

namespace HireSync.Domain.Tests;

public sealed class CvDocumentTests
{
    private static readonly DateTime UtcNow =
        new(
            2026,
            9,
            12,
            6,
            0,
            0,
            DateTimeKind.Utc);

    [Fact]
    public void Constructor_accepts_valid_pdf_at_exact_maximum_size()
    {
        var document =
            new CvDocument(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "candidate-cv.pdf",
                "11111111-1111-1111-1111-111111111111.pdf",
                "job-seeker/11111111-1111-1111-1111-111111111111.pdf",
                ".PDF",
                "application/pdf",
                CvDocument.MaxSizeBytes,
                new string('A', 64),
                UtcNow);

        Assert.Equal(
            CvDocument.PdfExtension,
            document.Extension);

        Assert.Equal(
            CvDocument.PdfContentType,
            document.ContentType);

        Assert.Equal(
            CvDocument.MaxSizeBytes,
            document.SizeBytes);

        Assert.Equal(
            new string('a', 64),
            document.Sha256Hash);

        Assert.Equal(
            UtcNow,
            document.UploadedAtUtc);
    }

    [Fact]
    public void Constructor_rejects_empty_document_id()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateValidDocument(
                    id:
                        Guid.Empty));
    }

    [Fact]
    public void Constructor_rejects_empty_profile_id()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateValidDocument(
                    profileId:
                        Guid.Empty));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5_000_001)]
    public void Constructor_rejects_invalid_size(
        long sizeBytes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateValidDocument(
                    sizeBytes:
                        sizeBytes));
    }

    [Fact]
    public void Constructor_rejects_unsupported_extension()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new CvDocument(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "candidate.txt",
                    "generated.txt",
                    "job-seeker/generated.txt",
                    ".txt",
                    "text/plain",
                    100,
                    new string('a', 64),
                    UtcNow));
    }

    [Fact]
    public void Constructor_rejects_content_type_mismatch()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new CvDocument(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "candidate.pdf",
                    "generated.pdf",
                    "job-seeker/generated.pdf",
                    ".pdf",
                    CvDocument.DocxContentType,
                    100,
                    new string('a', 64),
                    UtcNow));
    }

    [Fact]
    public void Constructor_rejects_invalid_sha256_hash()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateValidDocument(
                    sha256Hash:
                        "not-a-valid-sha256"));
    }

    [Fact]
    public void Constructor_rejects_non_utc_timestamp()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateValidDocument(
                    uploadedAtUtc:
                        DateTime.SpecifyKind(
                            UtcNow,
                            DateTimeKind.Local)));
    }

    [Fact]
    public void ReplaceFile_updates_metadata_but_preserves_identity_and_owner()
    {
        var id =
            Guid.NewGuid();

        var profileId =
            Guid.NewGuid();

        var document =
            CreateValidDocument(
                id:
                    id,
                profileId:
                    profileId);

        var replacementTime =
            UtcNow.AddMinutes(10);

        document.ReplaceFile(
            "replacement.docx",
            "22222222-2222-2222-2222-222222222222.docx",
            "job-seeker/22222222-2222-2222-2222-222222222222.docx",
            ".docx",
            CvDocument.DocxContentType,
            450_000,
            new string('b', 64),
            replacementTime);

        Assert.Equal(
            id,
            document.Id);

        Assert.Equal(
            profileId,
            document.JobSeekerProfileId);

        Assert.Equal(
            "replacement.docx",
            document.OriginalFileName);

        Assert.Equal(
            CvDocument.DocxExtension,
            document.Extension);

        Assert.Equal(
            CvDocument.DocxContentType,
            document.ContentType);

        Assert.Equal(
            450_000,
            document.SizeBytes);

        Assert.Equal(
            new string('b', 64),
            document.Sha256Hash);

        Assert.Equal(
            replacementTime,
            document.UploadedAtUtc);
    }

    [Fact]
    public void Constructor_rejects_parent_path_traversal()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateValidDocument(
                    relativeStoragePath:
                        "../outside/candidate.pdf"));
    }

    private static CvDocument CreateValidDocument(
        Guid? id = null,
        Guid? profileId = null,
        long sizeBytes = 100,
        string? sha256Hash = null,
        DateTime? uploadedAtUtc = null,
        string? relativeStoragePath = null)
    {
        return new CvDocument(
            id ?? Guid.NewGuid(),
            profileId ?? Guid.NewGuid(),
            "candidate.pdf",
            "11111111-1111-1111-1111-111111111111.pdf",
            relativeStoragePath ??
                "job-seeker/11111111-1111-1111-1111-111111111111.pdf",
            ".pdf",
            CvDocument.PdfContentType,
            sizeBytes,
            sha256Hash ??
                new string('a', 64),
            uploadedAtUtc ??
                UtcNow);
    }
}
