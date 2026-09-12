using HireSync.Application.DTOs;
using HireSync.Application.Interfaces.JobSeeker;
using HireSync.Application.Interfaces.Security;
using HireSync.Application.Interfaces.Storage;
using HireSync.Application.Interfaces.Time;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace HireSync.Api.IntegrationTests;

public sealed class JobSeekerCvServiceTests
{
    private static readonly DateTime FixedUtc =
        new(
            2026,
            9,
            12,
            6,
            0,
            0,
            DateTimeKind.Utc);

    [Fact]
    public async Task GetOwnCvAsync_returns_public_metadata_only()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(
                databaseName);

        var userId =
            Guid.NewGuid();

        var profileId =
            Guid.NewGuid();

        var cvId =
            Guid.NewGuid();

        await using (
            var seedContext =
                new HireSyncDbContext(
                    options))
        {
            seedContext.JobSeekerProfiles.Add(
                new JobSeekerProfile(
                    profileId,
                    userId,
                    FixedUtc));

            seedContext.CvDocuments.Add(
                CreateExistingCv(
                    cvId,
                    profileId));

            await seedContext.SaveChangesAsync();
        }

        await using var context =
            new HireSyncDbContext(
                options);

        var service =
            CreateService(
                context,
                userId,
                new FakeValidator(),
                new FakeFileStorage());

        var result =
            await service.GetOwnCvAsync();

        Assert.NotNull(result);

        Assert.Equal(
            "old.pdf",
            result!.OriginalFileName);

        Assert.Equal(
            ".pdf",
            result.Extension);

        var publicProperties =
            typeof(JobSeekerCvDto)
                .GetProperties()
                .Select(
                    property =>
                        property.Name)
                .ToArray();

        Assert.DoesNotContain(
            "StoredFileName",
            publicProperties);

        Assert.DoesNotContain(
            "RelativeStoragePath",
            publicProperties);

        Assert.DoesNotContain(
            "Sha256Hash",
            publicProperties);
    }

    [Fact]
    public async Task Upload_rejects_invalid_authenticated_user_before_storage()
    {
        var options =
            CreateOptions(
                Guid.NewGuid().ToString("N"));

        await using var context =
            new HireSyncDbContext(
                options);

        var validator =
            new FakeValidator();

        var storage =
            new FakeFileStorage();

        var service =
            new JobSeekerCvService(
                context,
                new FakeCurrentUser(
                    false,
                    null,
                    null),
                new FixedClock(),
                validator,
                storage,
                NullLogger<JobSeekerCvService>.Instance);

        var result =
            await service.UploadOrReplaceOwnCvAsync(
                CreateUploadRequest());

        Assert.False(
            result.Succeeded);

        Assert.Equal(
            CvOperationFailureReason
                .InvalidAuthenticatedUser,
            result.FailureReason);

        Assert.Equal(
            0,
            validator.CallCount);

        Assert.Equal(
            0,
            storage.StageCallCount);
    }

    [Fact]
    public async Task Upload_requires_existing_job_seeker_profile()
    {
        var options =
            CreateOptions(
                Guid.NewGuid().ToString("N"));

        await using var context =
            new HireSyncDbContext(
                options);

        var validator =
            new FakeValidator();

        var storage =
            new FakeFileStorage();

        var service =
            CreateService(
                context,
                Guid.NewGuid(),
                validator,
                storage);

        var result =
            await service.UploadOrReplaceOwnCvAsync(
                CreateUploadRequest());

        Assert.Equal(
            CvOperationFailureReason.ProfileNotFound,
            result.FailureReason);

        Assert.Equal(
            0,
            validator.CallCount);

        Assert.Equal(
            0,
            storage.StageCallCount);
    }

    [Fact]
    public async Task Upload_maps_file_too_large_without_staging()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(
                databaseName);

        var userId =
            Guid.NewGuid();

        await SeedProfileAsync(
            options,
            userId,
            Guid.NewGuid());

        await using var context =
            new HireSyncDbContext(
                options);

        var validator =
            new FakeValidator
            {
                Result =
                    CvFileValidationResult.Failure(
                        CvFileValidationFailureReason
                            .FileTooLarge,
                        5_000_001)
            };

        var storage =
            new FakeFileStorage();

        var service =
            CreateService(
                context,
                userId,
                validator,
                storage);

        var result =
            await service.UploadOrReplaceOwnCvAsync(
                CreateUploadRequest());

        Assert.Equal(
            CvOperationFailureReason.FileTooLarge,
            result.FailureReason);

        Assert.Equal(
            0,
            storage.StageCallCount);
    }

    [Fact]
    public async Task Upload_maps_invalid_file_type_separately_from_malformed_content()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(databaseName);

        var userId =
            Guid.NewGuid();

        await SeedProfileAsync(
            options,
            userId,
            Guid.NewGuid());

        await using var context =
            new HireSyncDbContext(options);

        var validator =
            new FakeValidator
            {
                Result =
                    CvFileValidationResult.Failure(
                        CvFileValidationFailureReason
                            .UnsupportedExtension)
            };

        var storage =
            new FakeFileStorage();

        var service =
            CreateService(
                context,
                userId,
                validator,
                storage);

        var result =
            await service.UploadOrReplaceOwnCvAsync(
                CreateUploadRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            CvOperationFailureReason.InvalidFileType,
            result.FailureReason);

        Assert.Equal(
            0,
            storage.StageCallCount);
    }

    [Fact]
    public async Task Upload_maps_malformed_file_separately_from_invalid_type()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(databaseName);

        var userId =
            Guid.NewGuid();

        await SeedProfileAsync(
            options,
            userId,
            Guid.NewGuid());

        await using var context =
            new HireSyncDbContext(options);

        var validator =
            new FakeValidator
            {
                Result =
                    CvFileValidationResult.Failure(
                        CvFileValidationFailureReason
                            .InvalidPdfSignature)
            };

        var storage =
            new FakeFileStorage();

        var service =
            CreateService(
                context,
                userId,
                validator,
                storage);

        var result =
            await service.UploadOrReplaceOwnCvAsync(
                CreateUploadRequest());

        Assert.False(result.Succeeded);

        Assert.Equal(
            CvOperationFailureReason.MalformedFile,
            result.FailureReason);

        Assert.Equal(
            0,
            storage.StageCallCount);
    }
    [Fact]
    public async Task First_upload_promotes_and_persists_one_current_cv()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(
                databaseName);

        var userId =
            Guid.NewGuid();

        var profileId =
            Guid.NewGuid();

        await SeedProfileAsync(
            options,
            userId,
            profileId);

        var storage =
            new FakeFileStorage();

        await using (
            var context =
                new HireSyncDbContext(
                    options))
        {
            var service =
                CreateService(
                    context,
                    userId,
                    new FakeValidator(),
                    storage);

            var result =
                await service.UploadOrReplaceOwnCvAsync(
                    CreateUploadRequest());

            Assert.True(
                result.Succeeded);

            Assert.NotNull(
                result.Cv);

            Assert.Equal(
                1,
                storage.StageCallCount);

            Assert.Equal(
                1,
                storage.PromoteCallCount);
        }

        await using var verificationContext =
            new HireSyncDbContext(
                options);

        var persisted =
            await verificationContext.CvDocuments
                .SingleAsync();

        Assert.Equal(
            profileId,
            persisted.JobSeekerProfileId);

        Assert.Equal(
            storage.StageResult.StoredFileName,
            persisted.StoredFileName);

        Assert.Equal(
            storage.StageResult.FinalRelativePath,
            persisted.RelativeStoragePath);

        Assert.Equal(
            storage.StageResult.Sha256Hash,
            persisted.Sha256Hash);

        Assert.Empty(
            storage.DeleteAttempts);
    }

    [Fact]
    public async Task Replacement_preserves_cv_identity_and_deletes_old_file_after_commit()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(
                databaseName);

        var userId =
            Guid.NewGuid();

        var profileId =
            Guid.NewGuid();

        var existingCvId =
            Guid.NewGuid();

        await SeedProfileAndCvAsync(
            options,
            userId,
            profileId,
            existingCvId);

        var storage =
            new FakeFileStorage();

        await using (
            var context =
                new HireSyncDbContext(
                    options))
        {
            var service =
                CreateService(
                    context,
                    userId,
                    new FakeValidator(),
                    storage);

            var result =
                await service.UploadOrReplaceOwnCvAsync(
                    CreateUploadRequest());

            Assert.True(
                result.Succeeded);
        }

        await using var verificationContext =
            new HireSyncDbContext(
                options);

        var persisted =
            await verificationContext.CvDocuments
                .SingleAsync();

        Assert.Equal(
            existingCvId,
            persisted.Id);

        Assert.Equal(
            storage.StageResult.FinalRelativePath,
            persisted.RelativeStoragePath);

        Assert.Contains(
            ExistingRelativePath,
            storage.DeleteAttempts);
    }

    [Fact]
    public async Task Persistence_failure_deletes_new_file_and_preserves_old_persisted_metadata()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(
                databaseName);

        var userId =
            Guid.NewGuid();

        var profileId =
            Guid.NewGuid();

        var existingCvId =
            Guid.NewGuid();

        await SeedProfileAndCvAsync(
            options,
            userId,
            profileId,
            existingCvId);

        var storage =
            new FakeFileStorage();

        await using (
            var failingContext =
                new FailingSaveHireSyncDbContext(
                    options))
        {
            failingContext.FailNextSave =
                true;

            var service =
                CreateService(
                    failingContext,
                    userId,
                    new FakeValidator(),
                    storage);

            var result =
                await service.UploadOrReplaceOwnCvAsync(
                    CreateUploadRequest());

            Assert.False(
                result.Succeeded);

            Assert.Equal(
                CvOperationFailureReason.PersistenceFailed,
                result.FailureReason);
        }

        Assert.Contains(
            storage.StageResult.FinalRelativePath,
            storage.DeleteAttempts);

        Assert.DoesNotContain(
            ExistingRelativePath,
            storage.DeleteAttempts);

        await using var verificationContext =
            new HireSyncDbContext(
                options);

        var persisted =
            await verificationContext.CvDocuments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            ExistingRelativePath,
            persisted.RelativeStoragePath);

        Assert.Equal(
            ExistingStoredFileName,
            persisted.StoredFileName);
    }

    [Fact]
    public async Task Old_file_delete_failure_does_not_roll_back_new_metadata()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(
                databaseName);

        var userId =
            Guid.NewGuid();

        var profileId =
            Guid.NewGuid();

        var existingCvId =
            Guid.NewGuid();

        await SeedProfileAndCvAsync(
            options,
            userId,
            profileId,
            existingCvId);

        var storage =
            new FakeFileStorage
            {
                ThrowOnDeletePath =
                    ExistingRelativePath
            };

        await using (
            var context =
                new HireSyncDbContext(
                    options))
        {
            var service =
                CreateService(
                    context,
                    userId,
                    new FakeValidator(),
                    storage);

            var result =
                await service.UploadOrReplaceOwnCvAsync(
                    CreateUploadRequest());

            Assert.True(
                result.Succeeded);
        }

        Assert.Contains(
            ExistingRelativePath,
            storage.DeleteAttempts);

        await using var verificationContext =
            new HireSyncDbContext(
                options);

        var persisted =
            await verificationContext.CvDocuments
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            storage.StageResult.FinalRelativePath,
            persisted.RelativeStoragePath);

        Assert.Equal(
            existingCvId,
            persisted.Id);
    }

    [Fact]
    public async Task Promotion_failure_cleans_staging_and_reports_storage_unavailable()
    {
        var databaseName =
            Guid.NewGuid().ToString("N");

        var options =
            CreateOptions(
                databaseName);

        var userId =
            Guid.NewGuid();

        await SeedProfileAsync(
            options,
            userId,
            Guid.NewGuid());

        var storage =
            new FakeFileStorage
            {
                ThrowOnPromote =
                    true
            };

        await using var context =
            new HireSyncDbContext(
                options);

        var service =
            CreateService(
                context,
                userId,
                new FakeValidator(),
                storage);

        var result =
            await service.UploadOrReplaceOwnCvAsync(
                CreateUploadRequest());

        Assert.Equal(
            CvOperationFailureReason.StorageUnavailable,
            result.FailureReason);

        Assert.Contains(
            storage.StageResult.StagingRelativePath,
            storage.DeleteAttempts);

        Assert.Empty(
            await context.CvDocuments
                .AsNoTracking()
                .ToListAsync());
    }

    private const string ExistingStoredFileName =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.pdf";

    private const string ExistingRelativePath =
        "aa/aa/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.pdf";

    private static DbContextOptions<HireSyncDbContext>
        CreateOptions(
            string databaseName)
    {
        return new DbContextOptionsBuilder<HireSyncDbContext>()
            .UseInMemoryDatabase(
                databaseName)
            .ConfigureWarnings(
                warnings =>
                    warnings.Ignore(
                        InMemoryEventId
                            .TransactionIgnoredWarning))
            .Options;
    }

    private static async Task SeedProfileAsync(
        DbContextOptions<HireSyncDbContext> options,
        Guid userId,
        Guid profileId)
    {
        await using var context =
            new HireSyncDbContext(
                options);

        context.JobSeekerProfiles.Add(
            new JobSeekerProfile(
                profileId,
                userId,
                FixedUtc));

        await context.SaveChangesAsync();
    }

    private static async Task SeedProfileAndCvAsync(
        DbContextOptions<HireSyncDbContext> options,
        Guid userId,
        Guid profileId,
        Guid cvId)
    {
        await using var context =
            new HireSyncDbContext(
                options);

        context.JobSeekerProfiles.Add(
            new JobSeekerProfile(
                profileId,
                userId,
                FixedUtc));

        context.CvDocuments.Add(
            CreateExistingCv(
                cvId,
                profileId));

        await context.SaveChangesAsync();
    }

    private static CvDocument CreateExistingCv(
        Guid cvId,
        Guid profileId)
    {
        return new CvDocument(
            cvId,
            profileId,
            "old.pdf",
            ExistingStoredFileName,
            ExistingRelativePath,
            ".pdf",
            CvDocument.PdfContentType,
            100,
            new string(
                'a',
                64),
            FixedUtc);
    }

    private static CvUploadRequest CreateUploadRequest()
    {
        return new CvUploadRequest(
            new MemoryStream(
                "%PDF-1.7 synthetic"u8
                    .ToArray()),
            "resume.pdf",
            CvDocument.PdfContentType);
    }

    private static JobSeekerCvService CreateService(
        HireSyncDbContext context,
        Guid userId,
        ICvFileValidator validator,
        IFileStorage storage)
    {
        return new JobSeekerCvService(
            context,
            new FakeCurrentUser(
                true,
                userId,
                RoleNames.JobSeeker),
            new FixedClock(),
            validator,
            storage,
            NullLogger<JobSeekerCvService>.Instance);
    }

    private sealed class FakeCurrentUser
        : ICurrentUser
    {
        public FakeCurrentUser(
            bool isAuthenticated,
            Guid? userId,
            string? role)
        {
            IsAuthenticated =
                isAuthenticated;

            UserId =
                userId;

            Role =
                role;
        }

        public bool IsAuthenticated
        {
            get;
        }

        public Guid? UserId
        {
            get;
        }

        public string? Role
        {
            get;
        }

        public string? Email =>
            "candidate@example.test";
    }

    private sealed class FixedClock
        : IClock
    {
        public DateTime UtcNow =>
            FixedUtc.AddMinutes(
                5);
    }

    private sealed class FakeValidator
        : ICvFileValidator
    {
        public int CallCount
        {
            get;
            private set;
        }

        public CvFileValidationResult Result
        {
            get;
            set;
        } =
            CvFileValidationResult.Success(
                "resume.pdf",
                ".pdf",
                CvDocument.PdfContentType,
                18);

        public Task<CvFileValidationResult> ValidateAsync(
            Stream content,
            string originalFileName,
            string declaredContentType,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Result);
        }
    }

    private sealed class FakeFileStorage
        : IFileStorage
    {
        public StagedFileResult StageResult
        {
            get;
        } =
            new(
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.pdf",
                ".staging/bbbbbbbbbbbbbbbbbbbbbbbbbbbb.tmp",
                "bb/bb/bbbbbbbbbbbbbbbbbbbbbbbbbbbb.pdf",
                18,
                new string(
                    'b',
                    64));

        public int StageCallCount
        {
            get;
            private set;
        }

        public int PromoteCallCount
        {
            get;
            private set;
        }

        public bool ThrowOnPromote
        {
            get;
            set;
        }

        public string? ThrowOnDeletePath
        {
            get;
            set;
        }

        public List<string> DeleteAttempts
        {
            get;
        } =
            new();

        public Task<StagedFileResult> StageAsync(
            Stream source,
            string extension,
            long maximumBytes,
            CancellationToken cancellationToken = default)
        {
            StageCallCount++;

            return Task.FromResult(
                StageResult);
        }

        public Task PromoteAsync(
            StagedFileResult stagedFile,
            CancellationToken cancellationToken = default)
        {
            PromoteCallCount++;

            if (ThrowOnPromote)
            {
                throw new IOException(
                    "Synthetic promotion failure.");
            }

            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            Stream stream =
                new MemoryStream(
                    "%PDF-1.7 synthetic"u8
                        .ToArray());

            return Task.FromResult(
                stream);
        }

        public Task DeleteIfExistsAsync(
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            DeleteAttempts.Add(
                relativePath);

            if (string.Equals(
                    ThrowOnDeletePath,
                    relativePath,
                    StringComparison.Ordinal))
            {
                throw new IOException(
                    "Synthetic delete failure.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FailingSaveHireSyncDbContext
        : HireSyncDbContext
    {
        public FailingSaveHireSyncDbContext(
            DbContextOptions<HireSyncDbContext> options)
            : base(options)
        {
        }

        public bool FailNextSave
        {
            get;
            set;
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave =
                    false;

                throw new DbUpdateException(
                    "Synthetic persistence failure.");
            }

            return base.SaveChangesAsync(
                cancellationToken);
        }
    }
}
