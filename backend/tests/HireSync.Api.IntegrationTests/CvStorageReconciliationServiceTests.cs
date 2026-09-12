using HireSync.Application.Interfaces.Time;
using HireSync.Domain.Entities;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HireSync.Api.IntegrationTests;

public sealed class CvStorageReconciliationServiceTests
    : IDisposable
{
    private static readonly DateTime FixedUtc =
        new(
            2026,
            9,
            12,
            12,
            0,
            0,
            DateTimeKind.Utc);

    private readonly string _rootPath;

    public CvStorageReconciliationServiceTests()
    {
        _rootPath =
            Path.Combine(
                Path.GetTempPath(),
                "HireSync-Cv-Reconciliation-Tests",
                Guid.NewGuid()
                    .ToString("N"));

        Directory.CreateDirectory(
            _rootPath);

        Directory.CreateDirectory(
            Path.Combine(
                _rootPath,
                ".staging"));
    }

    [Fact]
    public async Task Startup_cleanup_deletes_stale_unreferenced_staging_file()
    {
        var identifier =
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        var absolutePath =
            CreateStagingFile(
                identifier,
                FixedUtc.AddHours(
                    -25));

        await using var context =
            CreateContext();

        var service =
            CreateService(
                context);

        var result =
            await service
                .ReconcileAtStartupAsync();

        Assert.False(
            File.Exists(
                absolutePath));

        Assert.Equal(
            1,
            result.DeletedStagingFiles);

        Assert.Equal(
            0,
            result.FailedDeletes);
    }

    [Fact]
    public async Task Startup_cleanup_keeps_fresh_staging_file()
    {
        var identifier =
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        var absolutePath =
            CreateStagingFile(
                identifier,
                FixedUtc.AddHours(
                    -23));

        await using var context =
            CreateContext();

        var service =
            CreateService(
                context);

        var result =
            await service
                .ReconcileAtStartupAsync();

        Assert.True(
            File.Exists(
                absolutePath));

        Assert.Equal(
            0,
            result.DeletedStagingFiles);
    }

    [Fact]
    public async Task Startup_cleanup_keeps_file_exactly_at_grace_boundary()
    {
        var identifier =
            "cccccccccccccccccccccccccccccccc";

        var absolutePath =
            CreateStagingFile(
                identifier,
                FixedUtc.Subtract(
                    CvStorageReconciliationService
                        .CleanupGracePeriod));

        await using var context =
            CreateContext();

        var service =
            CreateService(
                context);

        var result =
            await service
                .ReconcileAtStartupAsync();

        Assert.True(
            File.Exists(
                absolutePath));

        Assert.Equal(
            0,
            result.DeletedStagingFiles);
    }

    [Fact]
    public async Task Startup_cleanup_deletes_stale_unreferenced_final_file()
    {
        var identifier =
            "dddddddddddddddddddddddddddddddd";

        var absolutePath =
            CreateFinalFile(
                identifier,
                ".pdf",
                FixedUtc.AddHours(
                    -25));

        await using var context =
            CreateContext();

        var service =
            CreateService(
                context);

        var result =
            await service
                .ReconcileAtStartupAsync();

        Assert.False(
            File.Exists(
                absolutePath));

        Assert.Equal(
            1,
            result.DeletedFinalFiles);
    }

    [Fact]
    public async Task Startup_cleanup_never_deletes_stale_referenced_final_file()
    {
        var identifier =
            "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";

        var relativePath =
            CreateFinalRelativePath(
                identifier,
                ".pdf");

        var absolutePath =
            CreateFinalFile(
                identifier,
                ".pdf",
                FixedUtc.AddDays(
                    -7));

        await using var context =
            CreateContext();

        var userId =
            Guid.NewGuid();

        var profileId =
            Guid.NewGuid();

        context.JobSeekerProfiles.Add(
            new JobSeekerProfile(
                profileId,
                userId,
                FixedUtc));

        context.CvDocuments.Add(
            new CvDocument(
                Guid.NewGuid(),
                profileId,
                "resume.pdf",
                $"{identifier}.pdf",
                relativePath,
                ".pdf",
                CvDocument.PdfContentType,
                1,
                new string(
                    'a',
                    64),
                FixedUtc));

        await context.SaveChangesAsync();

        var service =
            CreateService(
                context);

        var result =
            await service
                .ReconcileAtStartupAsync();

        Assert.True(
            File.Exists(
                absolutePath));

        Assert.Equal(
            0,
            result.DeletedFinalFiles);
    }

    [Fact]
    public async Task Startup_cleanup_keeps_fresh_unreferenced_final_file()
    {
        var identifier =
            "ffffffffffffffffffffffffffffffff";

        var absolutePath =
            CreateFinalFile(
                identifier,
                ".docx",
                FixedUtc.AddHours(
                    -1));

        await using var context =
            CreateContext();

        var service =
            CreateService(
                context);

        var result =
            await service
                .ReconcileAtStartupAsync();

        Assert.True(
            File.Exists(
                absolutePath));

        Assert.Equal(
            0,
            result.DeletedFinalFiles);
    }

    [Fact]
    public async Task Startup_cleanup_ignores_non_generated_files()
    {
        var stagingPath =
            Path.Combine(
                _rootPath,
                ".staging",
                "not-generated.tmp");

        File.WriteAllText(
            stagingPath,
            "do not delete");

        File.SetLastWriteTimeUtc(
            stagingPath,
            FixedUtc.AddDays(
                -10));

        var firstDirectory =
            Path.Combine(
                _rootPath,
                "aa");

        var secondDirectory =
            Path.Combine(
                firstDirectory,
                "aa");

        Directory.CreateDirectory(
            secondDirectory);

        var finalPath =
            Path.Combine(
                secondDirectory,
                "not-generated.pdf");

        File.WriteAllText(
            finalPath,
            "do not delete");

        File.SetLastWriteTimeUtc(
            finalPath,
            FixedUtc.AddDays(
                -10));

        await using var context =
            CreateContext();

        var service =
            CreateService(
                context);

        var result =
            await service
                .ReconcileAtStartupAsync();

        Assert.True(
            File.Exists(
                stagingPath));

        Assert.True(
            File.Exists(
                finalPath));

        Assert.Equal(
            0,
            result.DeletedStagingFiles);

        Assert.Equal(
            0,
            result.DeletedFinalFiles);
    }

    [Fact]
    public async Task Startup_cleanup_is_idempotent()
    {
        CreateStagingFile(
            "11111111111111111111111111111111",
            FixedUtc.AddHours(
                -25));

        CreateFinalFile(
            "22222222222222222222222222222222",
            ".pdf",
            FixedUtc.AddHours(
                -25));

        await using var context =
            CreateContext();

        var service =
            CreateService(
                context);

        var first =
            await service
                .ReconcileAtStartupAsync();

        var second =
            await service
                .ReconcileAtStartupAsync();

        Assert.Equal(
            1,
            first.DeletedStagingFiles);

        Assert.Equal(
            1,
            first.DeletedFinalFiles);

        Assert.Equal(
            0,
            second.DeletedStagingFiles);

        Assert.Equal(
            0,
            second.DeletedFinalFiles);

        Assert.Equal(
            0,
            second.FailedDeletes);
    }

    public void Dispose()
    {
        if (Directory.Exists(
                _rootPath))
        {
            Directory.Delete(
                _rootPath,
                recursive:
                    true);
        }
    }

    private HireSyncDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<
                    HireSyncDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid()
                        .ToString("N"))
                .Options;

        return new HireSyncDbContext(
            options);
    }

    private CvStorageReconciliationService
        CreateService(
            HireSyncDbContext context)
    {
        return new CvStorageReconciliationService(
            context,
            new FixedClock(),
            new LocalFileStorageOptions
            {
                RootPath =
                    _rootPath
            },
            NullLogger<
                CvStorageReconciliationService>
                .Instance);
    }

    private string CreateStagingFile(
        string identifier,
        DateTime lastWriteUtc)
    {
        var absolutePath =
            Path.Combine(
                _rootPath,
                ".staging",
                $"{identifier}.tmp");

        File.WriteAllText(
            absolutePath,
            "synthetic staging");

        File.SetLastWriteTimeUtc(
            absolutePath,
            lastWriteUtc);

        return absolutePath;
    }

    private string CreateFinalFile(
        string identifier,
        string extension,
        DateTime lastWriteUtc)
    {
        var relativePath =
            CreateFinalRelativePath(
                identifier,
                extension);

        var absolutePath =
            Path.Combine(
                _rootPath,
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));

        var directory =
            Path.GetDirectoryName(
                absolutePath)!;

        Directory.CreateDirectory(
            directory);

        File.WriteAllText(
            absolutePath,
            "synthetic protected CV");

        File.SetLastWriteTimeUtc(
            absolutePath,
            lastWriteUtc);

        return absolutePath;
    }

    private static string CreateFinalRelativePath(
        string identifier,
        string extension)
    {
        return $"{identifier[..2]}/{identifier.Substring(2, 2)}/{identifier}{extension}";
    }

    private sealed class FixedClock
        : IClock
    {
        public DateTime UtcNow =>
            FixedUtc;
    }
}
