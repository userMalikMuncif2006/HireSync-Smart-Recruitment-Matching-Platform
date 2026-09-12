namespace HireSync.Domain.Entities;

public sealed class CvDocument
{
    public const long MaxSizeBytes = 5_000_000;

    public const int MaxOriginalFileNameLength = 255;

    public const int MaxStoredFileNameLength = 80;

    public const int MaxRelativeStoragePathLength = 260;

    public const int MaxExtensionLength = 10;

    public const int MaxContentTypeLength = 100;

    public const int Sha256HashLength = 64;

    public const string PdfExtension = ".pdf";

    public const string DocxExtension = ".docx";

    public const string PdfContentType =
        "application/pdf";

    public const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public Guid Id { get; private set; }

    public Guid JobSeekerProfileId { get; private set; }

    public string OriginalFileName { get; private set; } =
        string.Empty;

    public string StoredFileName { get; private set; } =
        string.Empty;

    public string RelativeStoragePath { get; private set; } =
        string.Empty;

    public string Extension { get; private set; } =
        string.Empty;

    public string ContentType { get; private set; } =
        string.Empty;

    public long SizeBytes { get; private set; }

    public string Sha256Hash { get; private set; } =
        string.Empty;

    public DateTime UploadedAtUtc { get; private set; }

    private CvDocument()
    {
    }

    public CvDocument(
        Guid id,
        Guid jobSeekerProfileId,
        string originalFileName,
        string storedFileName,
        string relativeStoragePath,
        string extension,
        string contentType,
        long sizeBytes,
        string sha256Hash,
        DateTime uploadedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "CV document ID cannot be empty.",
                nameof(id));
        }

        if (jobSeekerProfileId == Guid.Empty)
        {
            throw new ArgumentException(
                "Job Seeker profile ID cannot be empty.",
                nameof(jobSeekerProfileId));
        }

        var metadata = ValidateMetadata(
            originalFileName,
            storedFileName,
            relativeStoragePath,
            extension,
            contentType,
            sizeBytes,
            sha256Hash,
            uploadedAtUtc);

        Id = id;
        JobSeekerProfileId = jobSeekerProfileId;

        ApplyMetadata(metadata);
    }

    public void ReplaceFile(
        string originalFileName,
        string storedFileName,
        string relativeStoragePath,
        string extension,
        string contentType,
        long sizeBytes,
        string sha256Hash,
        DateTime uploadedAtUtc)
    {
        var metadata = ValidateMetadata(
            originalFileName,
            storedFileName,
            relativeStoragePath,
            extension,
            contentType,
            sizeBytes,
            sha256Hash,
            uploadedAtUtc);

        ApplyMetadata(metadata);
    }

    private void ApplyMetadata(
        CvMetadata metadata)
    {
        OriginalFileName =
            metadata.OriginalFileName;

        StoredFileName =
            metadata.StoredFileName;

        RelativeStoragePath =
            metadata.RelativeStoragePath;

        Extension =
            metadata.Extension;

        ContentType =
            metadata.ContentType;

        SizeBytes =
            metadata.SizeBytes;

        Sha256Hash =
            metadata.Sha256Hash;

        UploadedAtUtc =
            metadata.UploadedAtUtc;
    }

    private static CvMetadata ValidateMetadata(
        string originalFileName,
        string storedFileName,
        string relativeStoragePath,
        string extension,
        string contentType,
        long sizeBytes,
        string sha256Hash,
        DateTime uploadedAtUtc)
    {
        var validatedOriginalFileName =
            RequireText(
                originalFileName,
                MaxOriginalFileNameLength,
                nameof(originalFileName));

        var validatedStoredFileName =
            RequireText(
                storedFileName,
                MaxStoredFileNameLength,
                nameof(storedFileName));

        if (validatedStoredFileName.Contains('/') ||
            validatedStoredFileName.Contains('\\'))
        {
            throw new ArgumentException(
                "Stored CV filename must not contain a path.",
                nameof(storedFileName));
        }

        var validatedRelativeStoragePath =
            RequireText(
                relativeStoragePath,
                MaxRelativeStoragePathLength,
                nameof(relativeStoragePath));

        ValidateRelativeStoragePath(
            validatedRelativeStoragePath,
            nameof(relativeStoragePath));

        var validatedExtension =
            RequireText(
                extension,
                MaxExtensionLength,
                nameof(extension))
            .ToLowerInvariant();

        if (validatedExtension is not PdfExtension and
            not DocxExtension)
        {
            throw new ArgumentException(
                "CV extension must be .pdf or .docx.",
                nameof(extension));
        }

        if (!validatedStoredFileName.EndsWith(
                validatedExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Stored CV filename extension does not match the approved extension.",
                nameof(storedFileName));
        }

        var validatedContentType =
            RequireText(
                contentType,
                MaxContentTypeLength,
                nameof(contentType))
            .ToLowerInvariant();

        var expectedContentType =
            validatedExtension == PdfExtension
                ? PdfContentType
                : DocxContentType;

        if (!string.Equals(
                validatedContentType,
                expectedContentType,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "CV content type does not match the approved extension.",
                nameof(contentType));
        }

        if (sizeBytes is < 1 or > MaxSizeBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeBytes),
                $"CV size must be between 1 and {MaxSizeBytes} bytes.");
        }

        var validatedHash =
            RequireText(
                sha256Hash,
                Sha256HashLength,
                nameof(sha256Hash))
            .ToLowerInvariant();

        if (validatedHash.Length != Sha256HashLength ||
            validatedHash.Any(
                character =>
                    !IsHexCharacter(character)))
        {
            throw new ArgumentException(
                "CV SHA-256 hash must contain exactly 64 hexadecimal characters.",
                nameof(sha256Hash));
        }

        EnsureUtc(
            uploadedAtUtc,
            nameof(uploadedAtUtc));

        return new CvMetadata(
            validatedOriginalFileName,
            validatedStoredFileName,
            validatedRelativeStoragePath,
            validatedExtension,
            validatedContentType,
            sizeBytes,
            validatedHash,
            uploadedAtUtc);
    }

    private static string RequireText(
        string value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value is required.",
                parameterName);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return trimmed;
    }

    private static void ValidateRelativeStoragePath(
        string relativeStoragePath,
        string parameterName)
    {
        if (relativeStoragePath.StartsWith(
                "/",
                StringComparison.Ordinal) ||
            relativeStoragePath.StartsWith(
                "\\",
                StringComparison.Ordinal) ||
            relativeStoragePath.Contains(':'))
        {
            throw new ArgumentException(
                "CV storage path must be relative.",
                parameterName);
        }

        var segments =
            relativeStoragePath.Split(
                new[] { '/', '\\' },
                StringSplitOptions.RemoveEmptyEntries);

        if (segments.Any(
                segment =>
                    segment == ".."))
        {
            throw new ArgumentException(
                "CV storage path cannot contain parent traversal.",
                parameterName);
        }
    }

    private static bool IsHexCharacter(
        char value)
    {
        return value is >= '0' and <= '9'
            or >= 'a' and <= 'f';
    }

    private static void EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Timestamp must be UTC.",
                parameterName);
        }
    }

    private sealed record CvMetadata(
        string OriginalFileName,
        string StoredFileName,
        string RelativeStoragePath,
        string Extension,
        string ContentType,
        long SizeBytes,
        string Sha256Hash,
        DateTime UploadedAtUtc);
}
