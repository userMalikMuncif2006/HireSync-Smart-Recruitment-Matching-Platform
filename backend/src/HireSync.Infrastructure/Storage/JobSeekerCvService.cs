using System.Data.Common;
using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.JobSeeker;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Storage;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireSync.Infrastructure.Storage;

public sealed class JobSeekerCvService
    : IJobSeekerCvService
{
    private readonly HireSyncDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ICvFileValidator _fileValidator;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<JobSeekerCvService> _logger;

    public JobSeekerCvService(
        HireSyncDbContext dbContext,
        ICurrentUser currentUser,
        IClock clock,
        ICvFileValidator fileValidator,
        IFileStorage fileStorage,
        ILogger<JobSeekerCvService> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
        _fileValidator = fileValidator;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<JobSeekerCvDto?> GetOwnCvAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetJobSeekerUserId(
                out var userId))
        {
            return null;
        }

        var profileId =
            await _dbContext.JobSeekerProfiles
                .AsNoTracking()
                .Where(
                    profile =>
                        profile.UserId == userId)
                .Select(
                    profile =>
                        (Guid?)profile.Id)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (!profileId.HasValue)
        {
            return null;
        }

        return await _dbContext.CvDocuments
            .AsNoTracking()
            .Where(
                cv =>
                    cv.JobSeekerProfileId ==
                    profileId.Value)
            .Select(
                cv =>
                    new JobSeekerCvDto(
                        cv.OriginalFileName,
                        cv.Extension,
                        cv.ContentType,
                        cv.SizeBytes,
                        cv.UploadedAtUtc))
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public async Task<CvDownloadResult> DownloadOwnCvAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetJobSeekerUserId(
                out var userId))
        {
            return CvDownloadResult.Failure(
                CvDownloadFailureReason
                    .InvalidAuthenticatedUser);
        }

        var cvFile =
            await (
                from profile in
                    _dbContext.JobSeekerProfiles
                        .AsNoTracking()
                join cv in
                    _dbContext.CvDocuments
                        .AsNoTracking()
                    on profile.Id equals
                    cv.JobSeekerProfileId
                where profile.UserId ==
                    userId
                select new
                {
                    cv.OriginalFileName,
                    cv.ContentType,
                    cv.RelativeStoragePath
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (cvFile is null)
        {
            return CvDownloadResult.Failure(
                CvDownloadFailureReason
                    .NotFound);
        }

        try
        {
            var content =
                await _fileStorage.OpenReadAsync(
                    cvFile.RelativeStoragePath,
                    cancellationToken);

            return CvDownloadResult.Success(
                content,
                cvFile.OriginalFileName,
                cvFile.ContentType);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or InvalidOperationException)
        {
            return CvDownloadResult.Failure(
                CvDownloadFailureReason
                    .StorageUnavailable);
        }
    }
    public async Task<CvUploadResult> UploadOrReplaceOwnCvAsync(
        CvUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetJobSeekerUserId(
                out var userId))
        {
            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .InvalidAuthenticatedUser);
        }

        if (request is null ||
            request.Content is null ||
            string.IsNullOrWhiteSpace(
                request.OriginalFileName) ||
            string.IsNullOrWhiteSpace(
                request.DeclaredContentType))
        {
            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .InvalidInput);
        }

        var profile =
            await _dbContext.JobSeekerProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.UserId == userId,
                    cancellationToken);

        if (profile is null)
        {
            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .ProfileNotFound);
        }

        var preflightFailure =
            ResolveUploadPreflight(
                request.OriginalFileName,
                request.DeclaredContentType,
                out var safeOriginalFileName,
                out var approvedExtension,
                out var approvedContentType);

        if (preflightFailure !=
            CvOperationFailureReason.None)
        {
            return CvUploadResult.Failure(
                preflightFailure);
        }

        var operationReferenceId =
            Guid.NewGuid();

        StagedFileResult? stagedFile =
            null;

        var validatedOriginalFileName =
            string.Empty;

        var validatedExtension =
            string.Empty;

        var validatedContentType =
            string.Empty;

        try
        {
            stagedFile =
                await _fileStorage.StageAsync(
                    request.Content,
                    approvedExtension,
                    CvDocument.MaxSizeBytes,
                    cancellationToken);

            if (stagedFile.SizeBytes <= 0)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                return CvUploadResult.Failure(
                    CvOperationFailureReason
                        .InvalidInput);
            }

            CvFileValidationResult validation;

            await using (
                var stagedContent =
                    await _fileStorage.OpenReadAsync(
                        stagedFile.StagingRelativePath,
                        cancellationToken))
            {
                validation =
                    await _fileValidator.ValidateAsync(
                        stagedContent,
                        safeOriginalFileName,
                        approvedContentType,
                        cancellationToken);
            }

            if (!validation.Succeeded)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                return CvUploadResult.Failure(
                    MapValidationFailure(
                        validation.FailureReason));
            }

            if (validation.SafeOriginalFileName is null ||
                validation.Extension is null ||
                validation.ContentType is null)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                return CvUploadResult.Failure(
                    CvOperationFailureReason
                        .InvalidInput);
            }

            if (validation.SizeBytes !=
                stagedFile.SizeBytes)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                return CvUploadResult.Failure(
                    CvOperationFailureReason
                        .StorageUnavailable);
            }

            validatedOriginalFileName =
                validation.SafeOriginalFileName;

            validatedExtension =
                validation.Extension;

            validatedContentType =
                validation.ContentType;

            await _fileStorage.PromoteAsync(
                stagedFile,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (stagedFile is not null)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                await BestEffortDeleteAsync(
                    stagedFile.FinalRelativePath,
                    operationReferenceId);
            }

            throw;
        }
        catch (FileStorageSizeLimitExceededException)
        {
            if (stagedFile is not null)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);
            }

            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .FileTooLarge);
        }
        catch (ObjectDisposedException)
        {
            if (stagedFile is not null)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                await BestEffortDeleteAsync(
                    stagedFile.FinalRelativePath,
                    operationReferenceId);
            }

            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .InvalidInput);
        }
        catch (NotSupportedException)
        {
            if (stagedFile is not null)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                await BestEffortDeleteAsync(
                    stagedFile.FinalRelativePath,
                    operationReferenceId);
            }

            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .MalformedFile);
        }
        catch (Exception exception)
            when (exception is IOException
                or UnauthorizedAccessException)
        {
            if (stagedFile is not null)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                await BestEffortDeleteAsync(
                    stagedFile.FinalRelativePath,
                    operationReferenceId);
            }

            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .StorageUnavailable);
        }

        if (stagedFile is null ||
            string.IsNullOrWhiteSpace(
                validatedOriginalFileName) ||
            string.IsNullOrWhiteSpace(
                validatedExtension) ||
            string.IsNullOrWhiteSpace(
                validatedContentType))
        {
            if (stagedFile is not null)
            {
                await BestEffortDeleteAsync(
                    stagedFile.FinalRelativePath,
                    operationReferenceId);
            }

            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .PersistenceFailed);
        }

        string? previousRelativeStoragePath =
            null;

        CvDocument? cvDocument =
            null;

        try
        {
            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            cvDocument =
                await _dbContext.CvDocuments
                    .SingleOrDefaultAsync(
                        candidate =>
                            candidate.JobSeekerProfileId ==
                            profile.Id,
                        cancellationToken);

            if (cvDocument is null)
            {
                cvDocument =
                    new CvDocument(
                        operationReferenceId,
                        profile.Id,
                        validatedOriginalFileName,
                        stagedFile.StoredFileName,
                        stagedFile.FinalRelativePath,
                        validatedExtension,
                        validatedContentType,
                        stagedFile.SizeBytes,
                        stagedFile.Sha256Hash,
                        _clock.UtcNow);

                _dbContext.CvDocuments.Add(
                    cvDocument);
            }
            else
            {
                operationReferenceId =
                    cvDocument.Id;

                previousRelativeStoragePath =
                    cvDocument.RelativeStoragePath;

                cvDocument.ReplaceFile(
                    validatedOriginalFileName,
                    stagedFile.StoredFileName,
                    stagedFile.FinalRelativePath,
                    validatedExtension,
                    validatedContentType,
                    stagedFile.SizeBytes,
                    stagedFile.Sha256Hash,
                    _clock.UtcNow);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await BestEffortDeleteAsync(
                stagedFile.FinalRelativePath,
                operationReferenceId);

            throw;
        }
        catch (ArgumentException)
        {
            await BestEffortDeleteAsync(
                stagedFile.FinalRelativePath,
                operationReferenceId);

            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .InvalidInput);
        }
        catch (Exception exception)
            when (exception is DbUpdateException
                or DbException
                or InvalidOperationException)
        {
            await BestEffortDeleteAsync(
                stagedFile.FinalRelativePath,
                operationReferenceId);

            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .PersistenceFailed);
        }

        if (cvDocument is null)
        {
            await BestEffortDeleteAsync(
                stagedFile.FinalRelativePath,
                operationReferenceId);

            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .PersistenceFailed);
        }

        if (!string.IsNullOrWhiteSpace(
                previousRelativeStoragePath) &&
            !string.Equals(
                previousRelativeStoragePath,
                stagedFile.FinalRelativePath,
                StringComparison.Ordinal))
        {
            await BestEffortDeleteAsync(
                previousRelativeStoragePath,
                cvDocument.Id);
        }

        return CvUploadResult.Success(
            ToDto(
                cvDocument));
    }


    private static CvOperationFailureReason
        ResolveUploadPreflight(
            string originalFileName,
            string declaredContentType,
            out string safeOriginalFileName,
            out string approvedExtension,
            out string approvedContentType)
    {
        safeOriginalFileName =
            string.Empty;

        approvedExtension =
            string.Empty;

        approvedContentType =
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                originalFileName) ||
            string.IsNullOrWhiteSpace(
                declaredContentType))
        {
            return CvOperationFailureReason
                .InvalidInput;
        }

        var normalizedOriginalFileName =
            originalFileName
                .Replace(
                    '\\',
                    '/')
                .Trim();

        var lastSeparatorIndex =
            normalizedOriginalFileName
                .LastIndexOf('/');

        var basename =
            lastSeparatorIndex >= 0
                ? normalizedOriginalFileName[
                    (lastSeparatorIndex + 1)..]
                : normalizedOriginalFileName;

        basename =
            basename.Trim();

        if (string.IsNullOrWhiteSpace(
                basename) ||
            basename is "." or ".." ||
            basename.Length >
                CvDocument.MaxOriginalFileNameLength ||
            basename.Any(
                char.IsControl))
        {
            return CvOperationFailureReason
                .InvalidInput;
        }

        var extension =
            Path.GetExtension(
                    basename)
                .ToLowerInvariant();

        string expectedContentType;

        if (extension ==
            CvDocument.PdfExtension)
        {
            expectedContentType =
                CvDocument.PdfContentType;
        }
        else if (extension ==
            CvDocument.DocxExtension)
        {
            expectedContentType =
                CvDocument.DocxContentType;
        }
        else
        {
            return CvOperationFailureReason
                .InvalidFileType;
        }

        if (!string.Equals(
                declaredContentType.Trim(),
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            return CvOperationFailureReason
                .InvalidFileType;
        }

        safeOriginalFileName =
            basename;

        approvedExtension =
            extension;

        approvedContentType =
            expectedContentType;

        return CvOperationFailureReason.None;
    }

    private async Task BestEffortDeleteAsync(
        string relativeStoragePath,
        Guid safeReferenceId)
    {
        try
        {
            await _fileStorage.DeleteIfExistsAsync(
                relativeStoragePath,
                CancellationToken.None);
        }
        catch
        {
            // Never log paths, filenames, hashes, or file content.
            _logger.LogWarning(
                "Protected CV file cleanup failed for reference {CvReferenceId}.",
                safeReferenceId);
        }
    }

    private bool TryGetJobSeekerUserId(
        out Guid userId)
    {
        userId =
            Guid.Empty;

        if (!_currentUser.IsAuthenticated ||
            _currentUser.UserId is not
                Guid currentUserId ||
            currentUserId ==
                Guid.Empty ||
            !string.Equals(
                _currentUser.Role,
                RoleNames.JobSeeker,
                StringComparison.Ordinal))
        {
            return false;
        }

        userId =
            currentUserId;

        return true;
    }

    private static CvOperationFailureReason
        MapValidationFailure(
            CvFileValidationFailureReason failureReason)
    {
        return failureReason switch
        {
            CvFileValidationFailureReason
                    .FileTooLarge =>
                CvOperationFailureReason
                    .FileTooLarge,

            CvFileValidationFailureReason
                    .UnsupportedExtension or
            CvFileValidationFailureReason
                    .UnsupportedContentType =>
                CvOperationFailureReason
                    .InvalidFileType,

            CvFileValidationFailureReason
                    .UnreadableContent or
            CvFileValidationFailureReason
                    .InvalidPdfSignature or
            CvFileValidationFailureReason
                    .InvalidDocxPackage or
            CvFileValidationFailureReason
                    .UnsafeDocxEntryPath or
            CvFileValidationFailureReason
                    .DocxEntryCountExceeded or
            CvFileValidationFailureReason
                    .DocxEntrySizeExceeded or
            CvFileValidationFailureReason
                    .DocxTotalSizeExceeded or
            CvFileValidationFailureReason
                    .DocxCompressionRatioExceeded =>
                CvOperationFailureReason
                    .MalformedFile,

            _ =>
                CvOperationFailureReason
                    .InvalidInput
        };
    }

    private static JobSeekerCvDto ToDto(
        CvDocument cvDocument)
    {
        return new JobSeekerCvDto(
            cvDocument.OriginalFileName,
            cvDocument.Extension,
            cvDocument.ContentType,
            cvDocument.SizeBytes,
            cvDocument.UploadedAtUtc);
    }
}
