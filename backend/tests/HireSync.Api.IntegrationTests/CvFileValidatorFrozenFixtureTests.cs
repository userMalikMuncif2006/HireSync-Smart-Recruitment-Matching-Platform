using System.IO.Compression;
using System.Text;
using HireSync.Application.DTOs;
using HireSync.Domain.Entities;
using HireSync.Infrastructure.Storage;

namespace HireSync.Api.IntegrationTests;

public sealed class CvFileValidatorFrozenFixtureTests
{
    private readonly CvFileValidator _validator = new();

    [Fact]
    public async Task Renamed_text_payload_with_allowed_pdf_extension_and_mime_is_rejected()
    {
        await using var stream =
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    "plain text pretending to be a PDF"));

        var result =
            await _validator.ValidateAsync(
                stream,
                "resume.pdf",
                CvDocument.PdfContentType);

        Assert.False(result.Succeeded);

        Assert.Equal(
            CvFileValidationFailureReason.InvalidPdfSignature,
            result.FailureReason);
    }

    [Fact]
    public async Task Renamed_zip_payload_with_allowed_pdf_extension_and_mime_is_rejected()
    {
        await using var stream =
            CreateOrdinaryZip();

        var result =
            await _validator.ValidateAsync(
                stream,
                "resume.pdf",
                CvDocument.PdfContentType);

        Assert.False(result.Succeeded);

        Assert.Equal(
            CvFileValidationFailureReason.InvalidPdfSignature,
            result.FailureReason);
    }

    [Fact]
    public async Task Executable_like_payload_with_allowed_pdf_extension_and_mime_is_rejected()
    {
        var executableLikeBytes =
            new byte[]
            {
                0x4D,
                0x5A,
                0x90,
                0x00,
                0x03,
                0x00,
                0x00,
                0x00
            };

        await using var stream =
            new MemoryStream(
                executableLikeBytes);

        var result =
            await _validator.ValidateAsync(
                stream,
                "resume.pdf",
                CvDocument.PdfContentType);

        Assert.False(result.Succeeded);

        Assert.Equal(
            CvFileValidationFailureReason.InvalidPdfSignature,
            result.FailureReason);
    }

    [Fact]
    public async Task Unicode_quotes_and_path_like_original_name_is_safely_reduced_to_basename()
    {
        await using var stream =
            new MemoryStream(
                "%PDF-1.7 synthetic"u8.ToArray());

        var result =
            await _validator.ValidateAsync(
                stream,
                "../../Résumé \"final\".PDF",
                CvDocument.PdfContentType);

        Assert.True(result.Succeeded);

        Assert.Equal(
            "Résumé \"final\".PDF",
            result.SafeOriginalFileName);

        Assert.Equal(
            CvDocument.PdfExtension,
            result.Extension);
    }

    [Fact]
    public async Task Original_filename_over_255_characters_is_rejected()
    {
        await using var stream =
            new MemoryStream(
                "%PDF-1.7 synthetic"u8.ToArray());

        var originalFileName =
            new string(
                'a',
                252) +
            ".pdf";

        var result =
            await _validator.ValidateAsync(
                stream,
                originalFileName,
                CvDocument.PdfContentType);

        Assert.False(result.Succeeded);

        Assert.Equal(
            CvFileValidationFailureReason.InvalidOriginalFileName,
            result.FailureReason);
    }

    [Fact]
    public async Task Non_seekable_content_is_rejected_as_unreadable()
    {
        await using var inner =
            new MemoryStream(
                "%PDF-1.7 synthetic"u8.ToArray());

        await using var stream =
            new NonSeekableReadStream(
                inner);

        var result =
            await _validator.ValidateAsync(
                stream,
                "resume.pdf",
                CvDocument.PdfContentType);

        Assert.False(result.Succeeded);

        Assert.Equal(
            CvFileValidationFailureReason.UnreadableContent,
            result.FailureReason);
    }

    [Fact]
    public async Task Encrypted_flagged_docx_is_rejected()
    {
        await using var stream =
            CreateEncryptedFlaggedDocx();

        var result =
            await _validator.ValidateAsync(
                stream,
                "resume.docx",
                CvDocument.DocxContentType);

        Assert.False(result.Succeeded);

        Assert.Equal(
            CvFileValidationFailureReason.InvalidDocxPackage,
            result.FailureReason);
    }

    private static MemoryStream CreateOrdinaryZip()
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
            var entry =
                archive.CreateEntry(
                    "hello.txt");

            using var writer =
                new StreamWriter(
                    entry.Open());

            writer.Write(
                "ordinary zip");
        }

        stream.Position =
            0;

        return stream;
    }

    private static MemoryStream CreateEncryptedFlaggedDocx()
    {
        var validStream =
            new MemoryStream();

        using (
            var archive =
                new ZipArchive(
                    validStream,
                    ZipArchiveMode.Create,
                    leaveOpen:
                        true))
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

        var bytes =
            validStream.ToArray();

        SetEncryptionFlag(
            bytes,
            localHeader:
                true);

        SetEncryptionFlag(
            bytes,
            localHeader:
                false);

        return new MemoryStream(
            bytes);
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

    private static void SetEncryptionFlag(
        byte[] bytes,
        bool localHeader)
    {
        var signature =
            localHeader
                ? new byte[]
                {
                    0x50,
                    0x4B,
                    0x03,
                    0x04
                }
                : new byte[]
                {
                    0x50,
                    0x4B,
                    0x01,
                    0x02
                };

        var flagOffset =
            localHeader
                ? 6
                : 8;

        for (var index = 0;
            index <=
                bytes.Length -
                signature.Length;
            index++)
        {
            if (bytes[index] !=
                    signature[0] ||
                bytes[index + 1] !=
                    signature[1] ||
                bytes[index + 2] !=
                    signature[2] ||
                bytes[index + 3] !=
                    signature[3])
            {
                continue;
            }

            bytes[
                index +
                flagOffset] |=
                0x01;
        }
    }

    private sealed class NonSeekableReadStream
        : Stream
    {
        private readonly Stream _inner;

        public NonSeekableReadStream(
            Stream inner)
        {
            _inner =
                inner;
        }

        public override bool CanRead =>
            true;

        public override bool CanSeek =>
            false;

        public override bool CanWrite =>
            false;

        public override long Length =>
            throw new NotSupportedException();

        public override long Position
        {
            get =>
                throw new NotSupportedException();

            set =>
                throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            return _inner.Read(
                buffer,
                offset,
                count);
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return _inner.ReadAsync(
                buffer,
                cancellationToken);
        }

        public override long Seek(
            long offset,
            SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(
            long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(
            byte[] buffer,
            int offset,
            int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(
                disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();

            GC.SuppressFinalize(
                this);
        }
    }
}
