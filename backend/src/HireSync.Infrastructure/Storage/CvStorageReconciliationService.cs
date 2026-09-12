using HireSync.Application.Interfaces.Time;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireSync.Infrastructure.Storage;

public sealed record CvStorageReconciliationResult(
    int DeletedStagingFiles,
    int DeletedFinalFiles,
    int FailedDeletes);

public sealed class CvStorageReconciliationService
{
    private const string StagingDirectoryName =
        ".staging";

    public static readonly TimeSpan CleanupGracePeriod =
        TimeSpan.FromHours(
            24);

    private readonly HireSyncDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ILogger<CvStorageReconciliationService>
        _logger;

    private readonly string _rootPath;

    private readonly StringComparison
        _pathComparison;

    public CvStorageReconciliationService(
        HireSyncDbContext dbContext,
        IClock clock,
        LocalFileStorageOptions storageOptions,
        ILogger<CvStorageReconciliationService> logger)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        ArgumentNullException.ThrowIfNull(
            clock);

        ArgumentNullException.ThrowIfNull(
            storageOptions);

        ArgumentNullException.ThrowIfNull(
            logger);

        if (string.IsNullOrWhiteSpace(
                storageOptions.RootPath) ||
            !Path.IsPathFullyQualified(
                storageOptions.RootPath))
        {
            throw new ArgumentException(
                "Protected CV storage root must be an absolute path.",
                nameof(storageOptions));
        }

        _dbContext =
            dbContext;

        _clock =
            clock;

        _logger =
            logger;

        _rootPath =
            NormalizeRootPath(
                storageOptions.RootPath);

        _pathComparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
    }

    public async Task<CvStorageReconciliationResult>
        ReconcileAtStartupAsync(
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(
                _rootPath))
        {
            throw new InvalidOperationException(
                "Protected CV storage root does not exist.");
        }

        var utcNow =
            _clock.UtcNow;

        if (utcNow.Kind !=
            DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "CV storage reconciliation requires a UTC clock.");
        }

        var referencedPaths =
            await _dbContext.CvDocuments
                .AsNoTracking()
                .Select(
                    cv =>
                        cv.RelativeStoragePath)
                .ToListAsync(
                    cancellationToken);

        var canonicalReferencedPaths =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var referencedPath in
            referencedPaths)
        {
            var normalized =
                NormalizeRelativePath(
                    referencedPath);

            if (!TryParseGeneratedFinalPath(
                    normalized,
                    out _))
            {
                throw new InvalidOperationException(
                    "CV metadata contains a non-canonical protected storage path.");
            }

            canonicalReferencedPaths.Add(
                normalized);
        }

        var cutoffUtc =
            utcNow.Subtract(
                CleanupGracePeriod);

        var deletedStagingFiles =
            0;

        var deletedFinalFiles =
            0;

        var failedDeletes =
            0;

        ReconcileStagingFiles(
            canonicalReferencedPaths,
            cutoffUtc,
            cancellationToken,
            ref deletedStagingFiles,
            ref failedDeletes);

        ReconcileFinalFiles(
            canonicalReferencedPaths,
            cutoffUtc,
            cancellationToken,
            ref deletedFinalFiles,
            ref failedDeletes);

        _logger.LogInformation(
            "Protected CV startup reconciliation completed. Deleted staging: {DeletedStagingFiles}; deleted final: {DeletedFinalFiles}; failed deletes: {FailedDeletes}.",
            deletedStagingFiles,
            deletedFinalFiles,
            failedDeletes);

        return new CvStorageReconciliationResult(
            deletedStagingFiles,
            deletedFinalFiles,
            failedDeletes);
    }

    private void ReconcileStagingFiles(
        IReadOnlySet<string> referencedPaths,
        DateTime cutoffUtc,
        CancellationToken cancellationToken,
        ref int deletedFiles,
        ref int failedDeletes)
    {
        var stagingPath =
            Path.Combine(
                _rootPath,
                StagingDirectoryName);

        if (!Directory.Exists(
                stagingPath))
        {
            return;
        }

        if (IsReparsePoint(
                stagingPath))
        {
            throw new InvalidOperationException(
                "Protected CV staging directory must not be a reparse point.");
        }

        foreach (var absolutePath in
            Directory.EnumerateFiles(
                stagingPath,
                "*",
                SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName =
                Path.GetFileName(
                    absolutePath);

            if (!TryParseGeneratedStagingFileName(
                    fileName,
                    out var generatedIdentifier))
            {
                continue;
            }

            var relativePath =
                $"{StagingDirectoryName}/{fileName}";

            if (referencedPaths.Contains(
                    relativePath))
            {
                continue;
            }

            var safeAbsolutePath =
                ResolveCandidateUnderRoot(
                    relativePath);

            if (!IsOlderThanCutoff(
                    safeAbsolutePath,
                    cutoffUtc))
            {
                continue;
            }

            TryDeleteGeneratedFile(
                safeAbsolutePath,
                generatedIdentifier,
                "staging",
                ref deletedFiles,
                ref failedDeletes);
        }
    }

    private void ReconcileFinalFiles(
        IReadOnlySet<string> referencedPaths,
        DateTime cutoffUtc,
        CancellationToken cancellationToken,
        ref int deletedFiles,
        ref int failedDeletes)
    {
        foreach (var firstDirectory in
            Directory.EnumerateDirectories(
                _rootPath,
                "*",
                SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var firstName =
                Path.GetFileName(
                    firstDirectory);

            if (string.Equals(
                    firstName,
                    StagingDirectoryName,
                    StringComparison.OrdinalIgnoreCase) ||
                !IsTwoLowerHexCharacters(
                    firstName) ||
                IsReparsePoint(
                    firstDirectory))
            {
                continue;
            }

            foreach (var secondDirectory in
                Directory.EnumerateDirectories(
                    firstDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var secondName =
                    Path.GetFileName(
                        secondDirectory);

                if (!IsTwoLowerHexCharacters(
                        secondName) ||
                    IsReparsePoint(
                        secondDirectory))
                {
                    continue;
                }

                foreach (var absolutePath in
                    Directory.EnumerateFiles(
                        secondDirectory,
                        "*",
                        SearchOption.TopDirectoryOnly))
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    var fileName =
                        Path.GetFileName(
                            absolutePath);

                    var relativePath =
                        $"{firstName}/{secondName}/{fileName}";

                    if (!TryParseGeneratedFinalPath(
                            relativePath,
                            out var generatedIdentifier))
                    {
                        continue;
                    }

                    if (referencedPaths.Contains(
                            relativePath))
                    {
                        continue;
                    }

                    var safeAbsolutePath =
                        ResolveCandidateUnderRoot(
                            relativePath);

                    if (!IsOlderThanCutoff(
                            safeAbsolutePath,
                            cutoffUtc))
                    {
                        continue;
                    }

                    TryDeleteGeneratedFile(
                        safeAbsolutePath,
                        generatedIdentifier,
                        "final",
                        ref deletedFiles,
                        ref failedDeletes);
                }
            }
        }
    }

    private void TryDeleteGeneratedFile(
        string absolutePath,
        string generatedIdentifier,
        string category,
        ref int deletedFiles,
        ref int failedDeletes)
    {
        try
        {
            if (!File.Exists(
                    absolutePath))
            {
                return;
            }

            File.Delete(
                absolutePath);

            deletedFiles++;

            _logger.LogInformation(
                "Deleted unreferenced protected CV {Category} file {CvStorageIdentifier}.",
                category,
                generatedIdentifier);
        }
        catch (Exception exception)
            when (exception is IOException
                or UnauthorizedAccessException)
        {
            failedDeletes++;

            _logger.LogWarning(
                "Failed to delete unreferenced protected CV {Category} file {CvStorageIdentifier}.",
                category,
                generatedIdentifier);
        }
    }

    private string ResolveCandidateUnderRoot(
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(
                relativePath))
        {
            throw new ArgumentException(
                "CV cleanup relative path is required.",
                nameof(relativePath));
        }

        if (Path.IsPathFullyQualified(
                relativePath) ||
            Path.IsPathRooted(
                relativePath))
        {
            throw new ArgumentException(
                "CV cleanup path must be relative.",
                nameof(relativePath));
        }

        var normalizedRelativePath =
            relativePath
                .Replace(
                    '\\',
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
            throw new InvalidOperationException(
                "CV cleanup candidate escapes the protected storage root.");
        }

        return candidatePath;
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
    private static bool IsOlderThanCutoff(
        string absolutePath,
        DateTime cutoffUtc)
    {
        return File.GetLastWriteTimeUtc(
                absolutePath) <
            cutoffUtc;
    }

    private static bool TryParseGeneratedStagingFileName(
        string fileName,
        out string generatedIdentifier)
    {
        generatedIdentifier =
            string.Empty;

        if (!fileName.EndsWith(
                ".tmp",
                StringComparison.Ordinal))
        {
            return false;
        }

        var identifier =
            Path.GetFileNameWithoutExtension(
                fileName);

        if (!IsLowerGuidIdentifier(
                identifier))
        {
            return false;
        }

        generatedIdentifier =
            identifier;

        return true;
    }

    private static bool TryParseGeneratedFinalPath(
        string relativePath,
        out string generatedIdentifier)
    {
        generatedIdentifier =
            string.Empty;

        var normalized =
            NormalizeRelativePath(
                relativePath);

        var segments =
            normalized.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length !=
            3)
        {
            return false;
        }

        var firstPrefix =
            segments[0];

        var secondPrefix =
            segments[1];

        var storedFileName =
            segments[2];

        if (!IsTwoLowerHexCharacters(
                firstPrefix) ||
            !IsTwoLowerHexCharacters(
                secondPrefix))
        {
            return false;
        }

        if (!string.Equals(
                storedFileName,
                storedFileName.ToLowerInvariant(),
                StringComparison.Ordinal))
        {
            return false;
        }

        var extension =
            Path.GetExtension(
                storedFileName);

        if (extension is not
                ".pdf" and not
                ".docx")
        {
            return false;
        }

        var identifier =
            Path.GetFileNameWithoutExtension(
                storedFileName);

        if (!IsLowerGuidIdentifier(
                identifier))
        {
            return false;
        }

        if (!string.Equals(
                firstPrefix,
                identifier[..2],
                StringComparison.Ordinal) ||
            !string.Equals(
                secondPrefix,
                identifier.Substring(
                    2,
                    2),
                StringComparison.Ordinal))
        {
            return false;
        }

        generatedIdentifier =
            identifier;

        return true;
    }

    private static string NormalizeRelativePath(
        string relativePath)
    {
        if (string.IsNullOrWhiteSpace(
                relativePath))
        {
            return string.Empty;
        }

        return relativePath
            .Replace(
                '\\',
                '/')
            .Trim();
    }

    private static bool IsTwoLowerHexCharacters(
        string value)
    {
        return value.Length ==
                2 &&
            value.All(
                IsLowerHexCharacter);
    }

    private static bool IsLowerGuidIdentifier(
        string value)
    {
        return value.Length ==
                32 &&
            value.All(
                IsLowerHexCharacter);
    }

    private static bool IsLowerHexCharacter(
        char character)
    {
        return character is
            (>= '0' and <= '9')
            or
            (>= 'a' and <= 'f');
    }

    private static bool IsReparsePoint(
        string directoryPath)
    {
        return (
            File.GetAttributes(
                directoryPath) &
            FileAttributes.ReparsePoint) !=
            0;
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
}
