using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.JobSeeker;
using HireSync.Application.Validation;
using HireSync.Domain.Entities;

namespace HireSync.Infrastructure.Storage;

public sealed class CvFileValidator
    : ICvFileValidator
{
    private const int BufferSize =
        81_920;

    private static readonly byte[] PdfSignature =
        Encoding.ASCII.GetBytes(
            "%PDF-");

    public async Task<CvFileValidationResult> ValidateAsync(
        Stream content,
        string originalFileName,
        string declaredContentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            content);

        cancellationToken.ThrowIfCancellationRequested();

        var safeOriginalFileName =
            GetSafeOriginalFileName(
                originalFileName);

        if (safeOriginalFileName is null)
        {
            return CvFileValidationResult.Failure(
                CvFileValidationFailureReason
                    .InvalidOriginalFileName);
        }

        if (!content.CanRead ||
            !content.CanSeek)
        {
            return CvFileValidationResult.Failure(
                CvFileValidationFailureReason
                    .UnreadableContent);
        }

        long sizeBytes;

        try
        {
            sizeBytes =
                content.Length;
        }
        catch (NotSupportedException)
        {
            return CvFileValidationResult.Failure(
                CvFileValidationFailureReason
                    .UnreadableContent);
        }

        if (sizeBytes <= 0)
        {
            return CvFileValidationResult.Failure(
                CvFileValidationFailureReason
                    .EmptyFile,
                sizeBytes);
        }

        if (sizeBytes >
            CvValidationPolicy.MaxCvBytes)
        {
            return CvFileValidationResult.Failure(
                CvFileValidationFailureReason
                    .FileTooLarge,
                sizeBytes);
        }

        var extension =
            Path.GetExtension(
                    safeOriginalFileName)
                .ToLowerInvariant();

        if (extension is not
                CvDocument.PdfExtension and
            not CvDocument.DocxExtension)
        {
            return CvFileValidationResult.Failure(
                CvFileValidationFailureReason
                    .UnsupportedExtension,
                sizeBytes);
        }

        var expectedContentType =
            extension ==
            CvDocument.PdfExtension
                ? CvDocument.PdfContentType
                : CvDocument.DocxContentType;

        var normalizedContentType =
            declaredContentType?
                .Trim();

        if (!string.Equals(
                normalizedContentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            return CvFileValidationResult.Failure(
                CvFileValidationFailureReason
                    .UnsupportedContentType,
                sizeBytes);
        }

        var originalPosition =
            content.Position;

        try
        {
            content.Position =
                0;

            CvFileValidationFailureReason
                contentFailure;

            if (extension ==
                CvDocument.PdfExtension)
            {
                contentFailure =
                    await ValidatePdfAsync(
                        content,
                        cancellationToken);
            }
            else
            {
                contentFailure =
                    await ValidateDocxAsync(
                        content,
                        cancellationToken);
            }

            if (contentFailure !=
                CvFileValidationFailureReason.None)
            {
                return CvFileValidationResult.Failure(
                    contentFailure,
                    sizeBytes);
            }

            return CvFileValidationResult.Success(
                safeOriginalFileName,
                extension,
                expectedContentType,
                sizeBytes);
        }
        finally
        {
            if (content.CanSeek)
            {
                content.Position =
                    originalPosition;
            }
        }
    }

    private static async Task<
        CvFileValidationFailureReason>
        ValidatePdfAsync(
            Stream content,
            CancellationToken cancellationToken)
    {
        var signature =
            new byte[
                PdfSignature.Length];

        var totalRead =
            0;

        while (totalRead <
            signature.Length)
        {
            var bytesRead =
                await content.ReadAsync(
                    signature.AsMemory(
                        totalRead,
                        signature.Length -
                        totalRead),
                    cancellationToken);

            if (bytesRead == 0)
            {
                return CvFileValidationFailureReason
                    .InvalidPdfSignature;
            }

            totalRead +=
                bytesRead;
        }

        return signature.SequenceEqual(
                PdfSignature)
            ? CvFileValidationFailureReason.None
            : CvFileValidationFailureReason
                .InvalidPdfSignature;
    }

    private static async Task<
        CvFileValidationFailureReason>
        ValidateDocxAsync(
            Stream content,
            CancellationToken cancellationToken)
    {
        try
        {
            if (HasEncryptedZipEntryFlags(
                    content))
            {
                return CvFileValidationFailureReason
                    .InvalidDocxPackage;
            }

            content.Position =
                0;

            using var archive =
                new ZipArchive(
                    content,
                    ZipArchiveMode.Read,
                    leaveOpen:
                        true);

            if (archive.Entries.Count >
                CvValidationPolicy
                    .MaxDocxEntryCount)
            {
                return CvFileValidationFailureReason
                    .DocxEntryCountExceeded;
            }

            var hasContentTypes =
                false;

            var hasWordDocument =
                false;

            long declaredTotalBytes =
                0;

            long actualTotalBytes =
                0;

            var buffer =
                ArrayPool<byte>.Shared.Rent(
                    BufferSize);

            try
            {
                foreach (var entry in
                    archive.Entries)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    var normalizedEntryName =
                        NormalizeEntryName(
                            entry.FullName);

                    if (normalizedEntryName is null)
                    {
                        return CvFileValidationFailureReason
                            .UnsafeDocxEntryPath;
                    }

                    if (string.Equals(
                            normalizedEntryName,
                            "[Content_Types].xml",
                            StringComparison.Ordinal))
                    {
                        hasContentTypes =
                            true;
                    }

                    if (string.Equals(
                            normalizedEntryName,
                            "word/document.xml",
                            StringComparison.Ordinal))
                    {
                        hasWordDocument =
                            true;
                    }

                    var declaredEntryBytes =
                        entry.Length;

                    if (declaredEntryBytes >
                        CvValidationPolicy
                            .MaxDocxEntryUncompressedBytes)
                    {
                        return CvFileValidationFailureReason
                            .DocxEntrySizeExceeded;
                    }

                    if (declaredEntryBytes >
                        CvValidationPolicy
                            .MaxDocxTotalUncompressedBytes -
                        declaredTotalBytes)
                    {
                        return CvFileValidationFailureReason
                            .DocxTotalSizeExceeded;
                    }

                    declaredTotalBytes +=
                        declaredEntryBytes;

                    if (declaredEntryBytes > 0)
                    {
                        var compressedBytes =
                            entry.CompressedLength;

                        if (compressedBytes <= 0)
                        {
                            return CvFileValidationFailureReason
                                .DocxCompressionRatioExceeded;
                        }

                        var ratio =
                            (double)declaredEntryBytes /
                            compressedBytes;

                        if (ratio >
                            CvValidationPolicy
                                .MaxDocxCompressionRatio)
                        {
                            return CvFileValidationFailureReason
                                .DocxCompressionRatioExceeded;
                        }
                    }

                    await using var
                        entryStream =
                            entry.Open();

                    long actualEntryBytes =
                        0;

                    while (true)
                    {
                        var bytesRead =
                            await entryStream.ReadAsync(
                                buffer.AsMemory(
                                    0,
                                    buffer.Length),
                                cancellationToken);

                        if (bytesRead == 0)
                        {
                            break;
                        }

                        actualEntryBytes +=
                            bytesRead;

                        actualTotalBytes +=
                            bytesRead;

                        if (actualEntryBytes >
                            CvValidationPolicy
                                .MaxDocxEntryUncompressedBytes)
                        {
                            return CvFileValidationFailureReason
                                .DocxEntrySizeExceeded;
                        }

                        if (actualTotalBytes >
                            CvValidationPolicy
                                .MaxDocxTotalUncompressedBytes)
                        {
                            return CvFileValidationFailureReason
                                .DocxTotalSizeExceeded;
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(
                    buffer);
            }

            if (!hasContentTypes ||
                !hasWordDocument)
            {
                return CvFileValidationFailureReason
                    .InvalidDocxPackage;
            }

            return CvFileValidationFailureReason.None;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is InvalidDataException
                or IOException
                or NotSupportedException)
        {
            return CvFileValidationFailureReason
                .InvalidDocxPackage;
        }
    }

    private static bool HasEncryptedZipEntryFlags(
        Stream content)
    {
        const uint EndOfCentralDirectorySignature =
            0x06054B50;

        const uint CentralDirectoryHeaderSignature =
            0x02014B50;

        const uint LocalFileHeaderSignature =
            0x04034B50;

        const ushort EncryptedFlag =
            0x0001;

        const ushort StrongEncryptionFlag =
            0x0040;

        const int EndOfCentralDirectoryMinimumSize =
            22;

        const int CentralDirectoryHeaderSize =
            46;

        const int LocalFileHeaderSize =
            30;

        var originalPosition =
            content.Position;

        try
        {
            var endOfCentralDirectoryOffset =
                FindEndOfCentralDirectory(
                    content);

            if (endOfCentralDirectoryOffset <
                0)
            {
                throw new InvalidDataException(
                    "ZIP end-of-central-directory record was not found.");
            }

            content.Position =
                endOfCentralDirectoryOffset;

            var endRecord =
                new byte[
                    EndOfCentralDirectoryMinimumSize];

            content.ReadExactly(
                endRecord);

            var signature =
                BinaryPrimitives
                    .ReadUInt32LittleEndian(
                        endRecord.AsSpan(
                            0,
                            4));

            if (signature !=
                EndOfCentralDirectorySignature)
            {
                throw new InvalidDataException(
                    "ZIP end-of-central-directory record is invalid.");
            }

            var totalEntries =
                BinaryPrimitives
                    .ReadUInt16LittleEndian(
                        endRecord.AsSpan(
                            10,
                            2));

            var centralDirectorySize =
                BinaryPrimitives
                    .ReadUInt32LittleEndian(
                        endRecord.AsSpan(
                            12,
                            4));

            var centralDirectoryOffset =
                BinaryPrimitives
                    .ReadUInt32LittleEndian(
                        endRecord.AsSpan(
                            16,
                            4));

            if (totalEntries ==
                    ushort.MaxValue ||
                centralDirectorySize ==
                    uint.MaxValue ||
                centralDirectoryOffset ==
                    uint.MaxValue)
            {
                throw new InvalidDataException(
                    "ZIP64 containers are not accepted for CV DOCX validation.");
            }

            var centralDirectoryStart =
                (long)centralDirectoryOffset;

            long centralDirectoryEnd;

            try
            {
                centralDirectoryEnd =
                    checked(
                        centralDirectoryStart +
                        centralDirectorySize);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException(
                    "ZIP central-directory bounds are invalid.",
                    exception);
            }

            if (centralDirectoryStart <
                    0 ||
                centralDirectoryEnd >
                    content.Length)
            {
                throw new InvalidDataException(
                    "ZIP central-directory bounds are invalid.");
            }

            content.Position =
                centralDirectoryStart;

            for (var index = 0;
                index < totalEntries;
                index++)
            {
                var centralHeader =
                    new byte[
                        CentralDirectoryHeaderSize];

                content.ReadExactly(
                    centralHeader);

                var centralSignature =
                    BinaryPrimitives
                        .ReadUInt32LittleEndian(
                            centralHeader.AsSpan(
                                0,
                                4));

                if (centralSignature !=
                    CentralDirectoryHeaderSignature)
                {
                    throw new InvalidDataException(
                        "ZIP central-directory entry is invalid.");
                }

                var centralFlags =
                    BinaryPrimitives
                        .ReadUInt16LittleEndian(
                            centralHeader.AsSpan(
                                8,
                                2));

                if (IsEncryptedZipFlags(
                        centralFlags,
                        EncryptedFlag,
                        StrongEncryptionFlag))
                {
                    return true;
                }

                var fileNameLength =
                    BinaryPrimitives
                        .ReadUInt16LittleEndian(
                            centralHeader.AsSpan(
                                28,
                                2));

                var extraFieldLength =
                    BinaryPrimitives
                        .ReadUInt16LittleEndian(
                            centralHeader.AsSpan(
                                30,
                                2));

                var commentLength =
                    BinaryPrimitives
                        .ReadUInt16LittleEndian(
                            centralHeader.AsSpan(
                                32,
                                2));

                var localHeaderOffset =
                    BinaryPrimitives
                        .ReadUInt32LittleEndian(
                            centralHeader.AsSpan(
                                42,
                                4));

                long nextCentralEntryPosition;

                try
                {
                    nextCentralEntryPosition =
                        checked(
                            content.Position +
                            fileNameLength +
                            extraFieldLength +
                            commentLength);
                }
                catch (OverflowException exception)
                {
                    throw new InvalidDataException(
                        "ZIP central-directory entry bounds are invalid.",
                        exception);
                }

                if (nextCentralEntryPosition >
                        centralDirectoryEnd ||
                    localHeaderOffset >=
                        content.Length)
                {
                    throw new InvalidDataException(
                        "ZIP entry bounds are invalid.");
                }

                content.Position =
                    localHeaderOffset;

                var localHeader =
                    new byte[
                        LocalFileHeaderSize];

                content.ReadExactly(
                    localHeader);

                var localSignature =
                    BinaryPrimitives
                        .ReadUInt32LittleEndian(
                            localHeader.AsSpan(
                                0,
                                4));

                if (localSignature !=
                    LocalFileHeaderSignature)
                {
                    throw new InvalidDataException(
                        "ZIP local-file header is invalid.");
                }

                var localFlags =
                    BinaryPrimitives
                        .ReadUInt16LittleEndian(
                            localHeader.AsSpan(
                                6,
                                2));

                if (IsEncryptedZipFlags(
                        localFlags,
                        EncryptedFlag,
                        StrongEncryptionFlag))
                {
                    return true;
                }

                content.Position =
                    nextCentralEntryPosition;
            }

            return false;
        }
        finally
        {
            content.Position =
                originalPosition;
        }
    }

    private static bool IsEncryptedZipFlags(
        ushort flags,
        ushort encryptedFlag,
        ushort strongEncryptionFlag)
    {
        return
            (flags & encryptedFlag) !=
                0 ||
            (flags & strongEncryptionFlag) !=
                0;
    }

    private static long FindEndOfCentralDirectory(
        Stream content)
    {
        const uint EndOfCentralDirectorySignature =
            0x06054B50;

        const int MinimumRecordSize =
            22;

        const int MaximumCommentSize =
            ushort.MaxValue;

        var searchLength =
            (int)Math.Min(
                content.Length,
                MinimumRecordSize +
                MaximumCommentSize);

        if (searchLength <
            MinimumRecordSize)
        {
            return -1;
        }

        var searchStart =
            content.Length -
            searchLength;

        content.Position =
            searchStart;

        var searchBuffer =
            new byte[
                searchLength];

        content.ReadExactly(
            searchBuffer);

        for (var index =
                searchBuffer.Length -
                MinimumRecordSize;
            index >= 0;
            index--)
        {
            var signature =
                BinaryPrimitives
                    .ReadUInt32LittleEndian(
                        searchBuffer.AsSpan(
                            index,
                            4));

            if (signature !=
                EndOfCentralDirectorySignature)
            {
                continue;
            }

            var commentLength =
                BinaryPrimitives
                    .ReadUInt16LittleEndian(
                        searchBuffer.AsSpan(
                            index +
                            20,
                            2));

            if (index +
                    MinimumRecordSize +
                    commentLength !=
                searchBuffer.Length)
            {
                continue;
            }

            return searchStart +
                index;
        }

        return -1;
    }
    private static string?
        GetSafeOriginalFileName(
            string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(
                originalFileName))
        {
            return null;
        }

        var normalized =
            originalFileName
                .Replace('\\', '/')
                .Trim();

        var lastSeparatorIndex =
            normalized.LastIndexOf('/');

        var basename =
            lastSeparatorIndex >= 0
                ? normalized[
                    (lastSeparatorIndex + 1)..]
                : normalized;

        basename =
            basename.Trim();

        if (string.IsNullOrWhiteSpace(
                basename) ||
            basename is "." or ".." ||
            basename.Length >
                CvValidationPolicy
                    .MaxOriginalFileNameLength ||
            basename.Any(
                char.IsControl))
        {
            return null;
        }

        return basename;
    }

    private static string?
        NormalizeEntryName(
            string entryName)
    {
        if (string.IsNullOrWhiteSpace(
                entryName))
        {
            return null;
        }

        var normalized =
            entryName.Replace(
                '\\',
                '/');

        if (normalized.StartsWith(
                "/",
                StringComparison.Ordinal) ||
            normalized.Contains(
                ':'))
        {
            return null;
        }

        var segments =
            normalized.Split(
                '/',
                StringSplitOptions
                    .RemoveEmptyEntries);

        if (segments.Any(
                segment =>
                    segment == ".."))
        {
            return null;
        }

        return normalized;
    }
}
