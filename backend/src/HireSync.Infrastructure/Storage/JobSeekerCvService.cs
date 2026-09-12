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

        CvFileValidationResult validation;

        try
        {
            validation =
                await _fileValidator.ValidateAsync(
                    request.Content,
                    request.OriginalFileName,
                    request.DeclaredContentType,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException)
        {
            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .MalformedFile);
        }
        catch (NotSupportedException)
        {
            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .MalformedFile);
        }
        catch (ObjectDisposedException)
        {
            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .InvalidInput);
        }

        if (!validation.Succeeded)
        {
            return CvUploadResult.Failure(
                MapValidationFailure(
                    validation.FailureReason));
        }

        if (validation.SafeOriginalFileName is null ||
            validation.Extension is null ||
            validation.ContentType is null)
        {
            return CvUploadResult.Failure(
                CvOperationFailureReason
                    .InvalidInput);
        }

        var operationReferenceId =
            Guid.NewGuid();

        StagedFileResult? stagedFile =
            null;

        try
        {
            request.Content.Position =
                0;

            stagedFile =
                await _fileStorage.StageAsync(
                    request.Content,
                    validation.Extension,
                    CvDocument.MaxSizeBytes,
                    cancellationToken);

            if (stagedFile.SizeBytes !=
                validation.SizeBytes)
            {
                await BestEffortDeleteAsync(
                    stagedFile.StagingRelativePath,
                    operationReferenceId);

                return CvUploadResult.Failure(
                    CvOperationFailureReason
                        .StorageUnavailable);
            }

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
                        validation.SafeOriginalFileName,
                        stagedFile.StoredFileName,
                        stagedFile.FinalRelativePath,
                        validation.Extension,
                        validation.ContentType,
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
                    validation.SafeOriginalFileName,
                    stagedFile.StoredFileName,
                    stagedFile.FinalRelativePath,
                    validation.Extension,
                    validation.ContentType,
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
