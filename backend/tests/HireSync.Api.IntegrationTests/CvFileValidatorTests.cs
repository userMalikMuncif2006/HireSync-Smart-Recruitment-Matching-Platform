using System.IO.Compression;
using HireSync.Application.DTOs;
using HireSync.Application.Validation;
using HireSync.Domain.Entities;
using HireSync.Infrastructure.Storage;

namespace HireSync.Api.IntegrationTests;

public sealed class CvFileValidatorTests
{
    private readonly CvFileValidator
        _validator =
            new();

    [Fact]
    public async Task Valid_pdf_at_exact_maximum_size_is_accepted()
    {
        var content =
            new byte[
                CvValidationPolicy.MaxCvBytes];

        "%PDF-"u8.CopyTo(
            content);

        await using var stream =
            new MemoryStream(
                content);

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.PDF",
                CvDocument.PdfContentType);

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            "candidate.PDF",
            result.SafeOriginalFileName);

        Assert.Equal(
            CvDocument.PdfExtension,
            result.Extension);

        Assert.Equal(
            CvValidationPolicy.MaxCvBytes,
            result.SizeBytes);
    }

    [Fact]
    public async Task Valid_docx_is_accepted()
    {
        await using var stream =
            CreateDocx(
                archive =>
                {
                    AddTextEntry(
                        archive,
                        "[Content_Types].xml",
                        "<Types />");

                    AddTextEntry(
                        archive,
                        "word/document.xml",
                        "<document />");
                });

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.docx",
                CvDocument.DocxContentType);

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            CvDocument.DocxExtension,
            result.Extension);
    }

    [Fact]
    public async Task Path_like_original_name_is_reduced_to_safe_basename()
    {
        await using var stream =
            CreatePdf();

        var result =
            await _validator.ValidateAsync(
                stream,
                @"C:\fakepath\resume.pdf",
                CvDocument.PdfContentType);

        Assert.True(
            result.Succeeded);

        Assert.Equal(
            "resume.pdf",
            result.SafeOriginalFileName);
    }

    [Fact]
    public async Task Empty_file_is_rejected()
    {
        await using var stream =
            new MemoryStream();

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.pdf",
                CvDocument.PdfContentType);

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            CvFileValidationFailureReason.EmptyFile,
            result.FailureReason);
    }

    [Fact]
    public async Task File_above_exact_maximum_is_rejected()
    {
        var content =
            new byte[
                CvValidationPolicy.MaxCvBytes +
                1];

        "%PDF-"u8.CopyTo(
            content);

        await using var stream =
            new MemoryStream(
                content);

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.pdf",
                CvDocument.PdfContentType);

        Assert.Equal(
            CvFileValidationFailureReason.FileTooLarge,
            result.FailureReason);
    }

    [Fact]
    public async Task Unsupported_extension_is_rejected()
    {
        await using var stream =
            CreatePdf();

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.txt",
                "text/plain");

        Assert.Equal(
            CvFileValidationFailureReason.UnsupportedExtension,
            result.FailureReason);
    }

    [Fact]
    public async Task Mismatched_declared_mime_is_rejected()
    {
        await using var stream =
            CreatePdf();

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.pdf",
                CvDocument.DocxContentType);

        Assert.Equal(
            CvFileValidationFailureReason.UnsupportedContentType,
            result.FailureReason);
    }

    [Fact]
    public async Task Spoofed_pdf_signature_is_rejected()
    {
        await using var stream =
            new MemoryStream(
                "NOT-PDF"u8
                    .ToArray());

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.pdf",
                CvDocument.PdfContentType);

        Assert.Equal(
            CvFileValidationFailureReason.InvalidPdfSignature,
            result.FailureReason);
    }

    [Fact]
    public async Task Malformed_docx_is_rejected()
    {
        await using var stream =
            new MemoryStream(
                "not-a-zip-package"u8
                    .ToArray());

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.docx",
                CvDocument.DocxContentType);

        Assert.Equal(
            CvFileValidationFailureReason.InvalidDocxPackage,
            result.FailureReason);
    }

    [Fact]
    public async Task Docx_missing_required_package_entry_is_rejected()
    {
        await using var stream =
            CreateDocx(
                archive =>
                {
                    AddTextEntry(
                        archive,
                        "[Content_Types].xml",
                        "<Types />");
                });

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.docx",
                CvDocument.DocxContentType);

        Assert.Equal(
            CvFileValidationFailureReason.InvalidDocxPackage,
            result.FailureReason);
    }

    [Fact]
    public async Task Docx_with_unsafe_entry_path_is_rejected()
    {
        await using var stream =
            CreateDocx(
                archive =>
                {
                    AddRequiredEntries(
                        archive);

                    AddTextEntry(
                        archive,
                        "../evil.xml",
                        "<evil />");
                });

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.docx",
                CvDocument.DocxContentType);

        Assert.Equal(
            CvFileValidationFailureReason.UnsafeDocxEntryPath,
            result.FailureReason);
    }

    [Fact]
    public async Task Docx_above_entry_count_limit_is_rejected()
    {
        await using var stream =
            CreateDocx(
                archive =>
                {
                    AddRequiredEntries(
                        archive);

                    for (var index = 0;
                        index <
                            CvValidationPolicy
                                .MaxDocxEntryCount -
                            1;
                        index++)
                    {
                        AddTextEntry(
                            archive,
                            $"word/extra-{index}.xml",
                            string.Empty);
                    }
                });

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.docx",
                CvDocument.DocxContentType);

        Assert.Equal(
            CvFileValidationFailureReason.DocxEntryCountExceeded,
            result.FailureReason);
    }

    [Fact]
    public async Task Docx_above_individual_uncompressed_limit_is_rejected()
    {
        await using var stream =
            CreateDocx(
                archive =>
                {
                    AddRequiredEntries(
                        archive);

                    AddRepeatedEntry(
                        archive,
                        "word/large.bin",
                        CvValidationPolicy
                            .MaxDocxEntryUncompressedBytes +
                        1,
                        randomBinary:
                            false);
                });

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.docx",
                CvDocument.DocxContentType);

        Assert.Equal(
            CvFileValidationFailureReason.DocxEntrySizeExceeded,
            result.FailureReason);
    }

    [Fact]
    public async Task Docx_above_total_uncompressed_limit_is_rejected()
    {
        await using var stream =
            CreateDocx(
                archive =>
                {
                    AddRequiredEntries(
                        archive);

                    const int EntrySize =
                        8_500_000;

                    AddRepeatedEntry(
                        archive,
                        "word/part-1.bin",
                        EntrySize,
                        randomBinary:
                            true);

                    AddRepeatedEntry(
                        archive,
                        "word/part-2.bin",
                        EntrySize,
                        randomBinary:
                            true);

                    AddRepeatedEntry(
                        archive,
                        "word/part-3.bin",
                        EntrySize,
                        randomBinary:
                            true);
                });

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.docx",
                CvDocument.DocxContentType);

        Assert.Equal(
            CvFileValidationFailureReason.DocxTotalSizeExceeded,
            result.FailureReason);
    }

    [Fact]
    public async Task Docx_above_compression_ratio_limit_is_rejected()
    {
        await using var stream =
            CreateDocx(
                archive =>
                {
                    AddRequiredEntries(
                        archive);

                    AddRepeatedEntry(
                        archive,
                        "word/high-ratio.bin",
                        1_000_000,
                        randomBinary:
                            false);
                });

        var result =
            await _validator.ValidateAsync(
                stream,
                "candidate.docx",
                CvDocument.DocxContentType);

        Assert.Equal(
            CvFileValidationFailureReason.DocxCompressionRatioExceeded,
            result.FailureReason);
    }

    private static MemoryStream CreatePdf()
    {
        return new MemoryStream(
            "%PDF-1.7 synthetic"u8
                .ToArray());
    }

    private static MemoryStream CreateDocx(
        Action<ZipArchive> populate)
    {
        var stream =
            new MemoryStream();

        using (
            var archive =
                new ZipArchive(
                    stream,
                    ZipArchiveMode.Create,
                    leaveOpen:
                        true))
        {
            populate(
                archive);
        }

        stream.Position =
            0;

        return stream;
    }

    private static void AddRequiredEntries(
        ZipArchive archive)
    {
        AddTextEntry(
            archive,
            "[Content_Types].xml",
            "<Types />");

        AddTextEntry(
            archive,
            "word/document.xml",
            "<document />");
    }

    private static void AddTextEntry(
        ZipArchive archive,
        string name,
        string content)
    {
        var entry =
            archive.CreateEntry(
                name,
                CompressionLevel.Fastest);

        using var writer =
            new StreamWriter(
                entry.Open());

        writer.Write(
            content);
    }

    private static void AddRepeatedEntry(
        ZipArchive archive,
        string name,
        long uncompressedBytes,
        bool randomBinary)
    {
        var entry =
            archive.CreateEntry(
                name,
                CompressionLevel.Optimal);

        using var output =
            entry.Open();

        var buffer =
            new byte[
                81_920];

        var random =
            new Random(
                12345);

        long remaining =
            uncompressedBytes;

        while (remaining > 0)
        {
            var bytesToWrite =
                (int)Math.Min(
                    buffer.Length,
                    remaining);

            if (randomBinary)
            {
                random.NextBytes(
                    buffer);

                for (var index = 0;
                    index <
                        bytesToWrite;
                    index++)
                {
                    buffer[index] &=
                        0x01;
                }
            }
            else
            {
                Array.Clear(
                    buffer,
                    0,
                    bytesToWrite);
            }

            output.Write(
                buffer,
                0,
                bytesToWrite);

            remaining -=
                bytesToWrite;
        }
    }
}
