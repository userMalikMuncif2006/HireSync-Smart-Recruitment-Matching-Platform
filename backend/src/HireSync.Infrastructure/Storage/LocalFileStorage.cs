using System.Buffers;
using System.Security.Cryptography;
using HireSync.Application.Interfaces.Storage;

namespace HireSync.Infrastructure.Storage;

public sealed class LocalFileStorage
    : IFileStorage
{
    private const string StagingDirectoryName =
        ".staging";

    private const int CopyBufferSize =
        81_920;

    private readonly string _rootPath;

    private readonly StringComparison
        _pathComparison;

    public LocalFileStorage(
        LocalFileStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(
                options.RootPath))
        {
            throw new ArgumentException(
                "Protected file storage root is required.",
                nameof(options));
        }

        if (!Path.IsPathFullyQualified(
                options.RootPath))
        {
            throw new ArgumentException(
                "Protected file storage root must be an absolute path.",
                nameof(options));
        }

        _rootPath =
            NormalizeRootPath(
                options.RootPath);

        _pathComparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        InitializeAndVerifyStorageRoot();
    }

    public async Task<StagedFileResult> StageAsync(
        Stream source,
        string extension,
        long maximumBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        cancellationToken.ThrowIfCancellationRequested();

        if (!source.CanRead)
        {
            throw new ArgumentException(
                "Source stream must be readable.",
                nameof(source));
        }

        if (maximumBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumBytes),
                "Maximum file size must be greater than zero.");
        }

        var normalizedExtension =
            NormalizeExtension(
                extension);

        var generatedIdentifier =
            Guid.NewGuid()
                .ToString("N");

        var storedFileName =
            generatedIdentifier +
            normalizedExtension;

        var stagingRelativePath =
            $"{StagingDirectoryName}/{generatedIdentifier}.tmp";

        var firstPrefix =
            generatedIdentifier[..2];

        var secondPrefix =
            generatedIdentifier.Substring(
                2,
                2);

        var finalRelativePath =
            $"{firstPrefix}/{secondPrefix}/{storedFileName}";

        var stagingAbsolutePath =
            ResolveRelativePath(
                stagingRelativePath);

        var totalBytes =
            0L;

        var buffer =
            ArrayPool<byte>.Shared.Rent(
                CopyBufferSize);

        try
        {
            await using (
                var output =
                    new FileStream(
                        stagingAbsolutePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        CopyBufferSize,
                        FileOptions.Asynchronous |
                        FileOptions.SequentialScan))
            {
                while (true)
                {
                    var bytesRead =
                        await source.ReadAsync(
                            buffer.AsMemory(
                                0,
                                buffer.Length),
                            cancellationToken);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytes +=
                        bytesRead;

                    if (totalBytes >
                        maximumBytes)
                    {
                        throw new FileStorageSizeLimitExceededException(
                            maximumBytes);
                    }

                    await output.WriteAsync(
                        buffer.AsMemory(
                            0,
                            bytesRead),
                        cancellationToken);
                }

                await output.FlushAsync(
                    cancellationToken);
            }

            string sha256Hash;

            await using (
                var stagedReadStream =
                    new FileStream(
                        stagingAbsolutePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        CopyBufferSize,
                        FileOptions.Asynchronous |
                        FileOptions.SequentialScan))
            {
                var hash =
                    await SHA256.HashDataAsync(
                        stagedReadStream,
                        cancellationToken);

                sha256Hash =
                    Convert.ToHexString(hash)
                        .ToLowerInvariant();
            }

            return new StagedFileResult(
                storedFileName,
                stagingRelativePath,
                finalRelativePath,
                totalBytes,
                sha256Hash);
        }
        catch
        {
            TryDeleteFile(
                stagingAbsolutePath);

            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(
                buffer);
        }
    }

    public Task PromoteAsync(
        StagedFileResult stagedFile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            stagedFile);

        cancellationToken.ThrowIfCancellationRequested();

        EnsureGeneratedStagingPath(
            stagedFile.StagingRelativePath,
            stagedFile.StoredFileName);

        EnsureGeneratedFinalPath(
            stagedFile.FinalRelativePath,
            stagedFile.StoredFileName);

        var stagingAbsolutePath =
            ResolveRelativePath(
                stagedFile.StagingRelativePath);

        var finalAbsolutePath =
            ResolveRelativePath(
                stagedFile.FinalRelativePath);

        if (!File.Exists(
                stagingAbsolutePath))
        {
            throw new FileNotFoundException(
                "The staged file does not exist.");
        }

        if (File.Exists(
                finalAbsolutePath))
        {
            throw new IOException(
                "The generated final file already exists.");
        }

        var finalDirectory =
            Path.GetDirectoryName(
                finalAbsolutePath)
            ?? throw new IOException(
                "The generated final directory is invalid.");

        Directory.CreateDirectory(
            finalDirectory);

        File.Move(
            stagingAbsolutePath,
            finalAbsolutePath);

        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath =
            ResolveRelativePath(
                relativePath);

        Stream stream =
            new FileStream(
                absolutePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan);

        return Task.FromResult(
            stream);
    }

    public Task DeleteIfExistsAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath =
            ResolveRelativePath(
                relativePath);

        if (File.Exists(
                absolutePath))
        {
            File.Delete(
                absolutePath);
        }

        return Task.CompletedTask;
    }

    private void InitializeAndVerifyStorageRoot()
    {
        try
        {
            Directory.CreateDirectory(
                _rootPath);

            var stagingPath =
                ResolveRelativePath(
                    StagingDirectoryName);

            Directory.CreateDirectory(
                stagingPath);

            var probeRelativePath =
                $"{StagingDirectoryName}/storage-probe-{Guid.NewGuid():N}.tmp";

            var probeAbsolutePath =
                ResolveRelativePath(
                    probeRelativePath);

            try
            {
                using (
                    var writeStream =
                        new FileStream(
                            probeAbsolutePath,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None))
                {
                    writeStream.WriteByte(
                        0x48);

                    writeStream.Flush(
                        flushToDisk:
                            true);
                }

                using var readStream =
                    new FileStream(
                        probeAbsolutePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read);

                if (readStream.ReadByte() !=
                    0x48)
                {
                    throw new IOException(
                        "Protected storage read verification failed.");
                }
            }
            finally
            {
                TryDeleteFile(
                    probeAbsolutePath);
            }
        }
        catch (Exception exception)
            when (exception is IOException
                or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                "Protected file storage root is not readable and writable.",
                exception);
        }
    }

    private string ResolveRelativePath(
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(
                relativePath))
        {
            throw new ArgumentException(
                "Storage relative path is required.",
                nameof(relativePath));
        }

        if (Path.IsPathFullyQualified(
                relativePath) ||
            Path.IsPathRooted(
                relativePath))
        {
            throw new ArgumentException(
                "Storage path must be relative.",
                nameof(relativePath));
        }

        var normalizedRelativePath =
            relativePath
                .Replace(
                    Path.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar)
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);

        var candidatePath =
            Path.GetFullPath(
                Path.Combine(
                    _rootPath,
                    normalizedRelativePath));

        var requiredPrefix =
            EnsureTrailingDirectorySeparator(
                _rootPath);

        if (!candidatePath.StartsWith(
                requiredPrefix,
                _pathComparison))
        {
            throw new ArgumentException(
                "Storage path escapes the protected root.",
                nameof(relativePath));
        }

        return candidatePath;
    }

    private static string NormalizeRootPath(
        string rootPath)
    {
        var fullPath =
            Path.GetFullPath(
                rootPath);

        var pathRoot =
            Path.GetPathRoot(
                fullPath);

        if (!string.IsNullOrEmpty(
                pathRoot) &&
            string.Equals(
                fullPath,
                pathRoot,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            return fullPath;
        }

        return fullPath.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
    }

    private static string EnsureTrailingDirectorySeparator(
        string path)
    {
        if (path.EndsWith(
                Path.DirectorySeparatorChar) ||
            path.EndsWith(
                Path.AltDirectorySeparatorChar))
        {
            return path;
        }

        return path +
            Path.DirectorySeparatorChar;
    }

    private static string NormalizeExtension(
        string extension)
    {
        if (string.IsNullOrWhiteSpace(
                extension))
        {
            throw new ArgumentException(
                "File extension is required.",
                nameof(extension));
        }

        var normalized =
            extension.Trim()
                .ToLowerInvariant();

        if (!normalized.StartsWith(
                '.'))
        {
            throw new ArgumentException(
                "File extension must begin with a period.",
                nameof(extension));
        }

        if (normalized.Length >
            10)
        {
            throw new ArgumentException(
                "File extension is too long.",
                nameof(extension));
        }

        if (normalized.Contains('/') ||
            normalized.Contains('\\') ||
            normalized.Contains(':') ||
            normalized.Contains(
                "..",
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "File extension is invalid.",
                nameof(extension));
        }

        return normalized;
    }

    private static void EnsureGeneratedStagingPath(
        string relativePath,
        string storedFileName)
    {
        var generatedIdentifier =
            ValidateGeneratedStoredFileName(
                storedFileName);

        var normalized =
            NormalizeForContractCheck(
                relativePath);

        var expected =
            $"{StagingDirectoryName}/{generatedIdentifier}.tmp";

        if (!string.Equals(
                normalized,
                expected,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Staged file path is not storage-generated.",
                nameof(relativePath));
        }
    }

    private static void EnsureGeneratedFinalPath(
        string relativePath,
        string storedFileName)
    {
        var generatedIdentifier =
            ValidateGeneratedStoredFileName(
                storedFileName);

        var firstPrefix =
            generatedIdentifier[..2];

        var secondPrefix =
            generatedIdentifier.Substring(
                2,
                2);

        var expected =
            $"{firstPrefix}/{secondPrefix}/{storedFileName}";

        var normalized =
            NormalizeForContractCheck(
                relativePath);

        if (!string.Equals(
                normalized,
                expected,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Final file path is not storage-generated.",
                nameof(relativePath));
        }
    }

    private static string ValidateGeneratedStoredFileName(
        string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(
                storedFileName) ||
            storedFileName.Contains('/') ||
            storedFileName.Contains('\\') ||
            storedFileName.Contains(':'))
        {
            throw new ArgumentException(
                "Stored filename is invalid.",
                nameof(storedFileName));
        }

        if (!string.Equals(
                storedFileName,
                storedFileName.ToLowerInvariant(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Stored filename must be lowercase.",
                nameof(storedFileName));
        }

        var generatedIdentifier =
            Path.GetFileNameWithoutExtension(
                storedFileName);

        var extension =
            Path.GetExtension(
                storedFileName);

        if (generatedIdentifier.Length !=
                32 ||
            string.IsNullOrWhiteSpace(
                extension) ||
            generatedIdentifier.Any(
                character =>
                    character is not
                        (>= '0' and <= '9')
                    and not
                        (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "Stored filename is not a generated GUID filename.",
                nameof(storedFileName));
        }

        return generatedIdentifier;
    }

    private static string NormalizeForContractCheck(
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(
                relativePath))
        {
            throw new ArgumentException(
                "Storage relative path is required.",
                nameof(relativePath));
        }

        return relativePath
            .Replace('\\', '/')
            .Trim();
    }

    private static void TryDeleteFile(
        string absolutePath)
    {
        try
        {
            if (File.Exists(
                    absolutePath))
            {
                File.Delete(
                    absolutePath);
            }
        }
        catch
        {
            // Best-effort cleanup only.
            // The original failure remains authoritative.
        }
    }
}
