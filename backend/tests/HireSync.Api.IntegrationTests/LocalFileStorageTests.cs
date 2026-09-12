using System.Security.Cryptography;
using HireSync.Application.Interfaces.Storage;
using HireSync.Infrastructure.Storage;

namespace HireSync.Api.IntegrationTests;

public sealed class LocalFileStorageTests
    : IDisposable
{
    private readonly string _rootPath;

    public LocalFileStorageTests()
    {
        _rootPath =
            Path.Combine(
                Path.GetTempPath(),
                "HireSync-Storage-Tests",
                Guid.NewGuid()
                    .ToString("N"));

        Directory.CreateDirectory(
            _rootPath);
    }

    [Fact]
    public void Constructor_rejects_relative_root()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LocalFileStorage(
                    new LocalFileStorageOptions
                    {
                        RootPath =
                            "relative-storage-root"
                    }));
    }

    [Fact]
    public async Task StageAsync_creates_only_staging_file_and_returns_generated_metadata()
    {
        var storage =
            CreateStorage();

        var content =
            "synthetic cv content"u8
                .ToArray();

        await using var source =
            new MemoryStream(
                content);

        var result =
            await storage.StageAsync(
                source,
                ".PDF",
                5_000_000);

        Assert.EndsWith(
            ".pdf",
            result.StoredFileName,
            StringComparison.Ordinal);

        Assert.StartsWith(
            ".staging/",
            result.StagingRelativePath,
            StringComparison.Ordinal);

        Assert.Equal(
            $"files/{result.StoredFileName}",
            result.FinalRelativePath);

        Assert.Equal(
            content.LongLength,
            result.SizeBytes);

        var expectedHash =
            Convert.ToHexString(
                    SHA256.HashData(
                        content))
                .ToLowerInvariant();

        Assert.Equal(
            expectedHash,
            result.Sha256Hash);

        Assert.True(
            File.Exists(
                Resolve(
                    result.StagingRelativePath)));

        Assert.False(
            File.Exists(
                Resolve(
                    result.FinalRelativePath)));
    }

    [Fact]
    public async Task PromoteAsync_moves_staged_file_to_final_location()
    {
        var storage =
            CreateStorage();

        await using var source =
            new MemoryStream(
                "synthetic pdf"u8
                    .ToArray());

        var staged =
            await storage.StageAsync(
                source,
                ".pdf",
                5_000_000);

        await storage.PromoteAsync(
            staged);

        Assert.False(
            File.Exists(
                Resolve(
                    staged.StagingRelativePath)));

        Assert.True(
            File.Exists(
                Resolve(
                    staged.FinalRelativePath)));
    }

    [Fact]
    public async Task OpenReadAsync_returns_exact_promoted_bytes()
    {
        var storage =
            CreateStorage();

        var expected =
            "protected synthetic document"u8
                .ToArray();

        await using var source =
            new MemoryStream(
                expected);

        var staged =
            await storage.StageAsync(
                source,
                ".docx",
                5_000_000);

        await storage.PromoteAsync(
            staged);

        await using var opened =
            await storage.OpenReadAsync(
                staged.FinalRelativePath);

        using var destination =
            new MemoryStream();

        await opened.CopyToAsync(
            destination);

        Assert.Equal(
            expected,
            destination.ToArray());
    }

    [Fact]
    public async Task DeleteIfExistsAsync_removes_promoted_file()
    {
        var storage =
            CreateStorage();

        await using var source =
            new MemoryStream(
                "delete me"u8
                    .ToArray());

        var staged =
            await storage.StageAsync(
                source,
                ".pdf",
                5_000_000);

        await storage.PromoteAsync(
            staged);

        await storage.DeleteIfExistsAsync(
            staged.FinalRelativePath);

        Assert.False(
            File.Exists(
                Resolve(
                    staged.FinalRelativePath)));

        await storage.DeleteIfExistsAsync(
            staged.FinalRelativePath);
    }

    [Fact]
    public async Task OpenReadAsync_rejects_parent_traversal()
    {
        var storage =
            CreateStorage();

        await Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                await using var _ =
                    await storage.OpenReadAsync(
                        "../outside.pdf");
            });
    }

    [Fact]
    public async Task OpenReadAsync_rejects_absolute_path()
    {
        var storage =
            CreateStorage();

        var absolutePath =
            Path.Combine(
                Path.GetTempPath(),
                "outside.pdf");

        await Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                await using var _ =
                    await storage.OpenReadAsync(
                        absolutePath);
            });
    }

    [Fact]
    public async Task StageAsync_over_limit_removes_partial_staging_file()
    {
        var storage =
            CreateStorage();

        var content =
            new byte[101];

        await using var source =
            new MemoryStream(
                content);

        var exception =
            await Assert.ThrowsAsync<
                FileStorageSizeLimitExceededException>(
                () =>
                    storage.StageAsync(
                        source,
                        ".pdf",
                        100));

        Assert.Equal(
            100,
            exception.MaximumBytes);

        var stagingDirectory =
            Path.Combine(
                _rootPath,
                ".staging");

        Assert.Empty(
            Directory.EnumerateFiles(
                stagingDirectory));
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

    private LocalFileStorage CreateStorage()
    {
        return new LocalFileStorage(
            new LocalFileStorageOptions
            {
                RootPath =
                    _rootPath
            });
    }

    private string Resolve(
        string relativePath)
    {
        return Path.Combine(
            _rootPath,
            relativePath.Replace(
                '/',
                Path.DirectorySeparatorChar));
    }
}
