using HireSync.Infrastructure.Storage;

namespace HireSync.Api.IntegrationTests;

public sealed class ProtectedStorageRootValidatorTests
    : IDisposable
{
    private readonly string _sandboxRoot;

    public ProtectedStorageRootValidatorTests()
    {
        _sandboxRoot =
            Path.Combine(
                Path.GetTempPath(),
                "HireSync-Storage-Root-Tests",
                Guid.NewGuid()
                    .ToString("N"));

        Directory.CreateDirectory(
            _sandboxRoot);
    }

    [Fact]
    public void ValidateAndNormalize_accepts_absolute_root_outside_webroot()
    {
        var webRoot =
            Path.Combine(
                _sandboxRoot,
                "api",
                "wwwroot");

        var protectedRoot =
            Path.Combine(
                _sandboxRoot,
                "protected",
                "cv");

        var result =
            ProtectedStorageRootValidator
                .ValidateAndNormalize(
                    protectedRoot,
                    webRoot);

        Assert.Equal(
            Path.GetFullPath(
                protectedRoot),
            result);
    }

    [Fact]
    public void ValidateAndNormalize_rejects_public_webroot_itself()
    {
        var webRoot =
            Path.Combine(
                _sandboxRoot,
                "wwwroot");

        Assert.Throws<InvalidOperationException>(
            () =>
                ProtectedStorageRootValidator
                    .ValidateAndNormalize(
                        webRoot,
                        webRoot));
    }

    [Fact]
    public void ValidateAndNormalize_rejects_root_below_public_webroot()
    {
        var webRoot =
            Path.Combine(
                _sandboxRoot,
                "wwwroot");

        var unsafeRoot =
            Path.Combine(
                webRoot,
                "uploads",
                "cv");

        Assert.Throws<InvalidOperationException>(
            () =>
                ProtectedStorageRootValidator
                    .ValidateAndNormalize(
                        unsafeRoot,
                        webRoot));
    }

    [Fact]
    public void ValidateAndNormalize_rejects_relative_storage_root()
    {
        var webRoot =
            Path.Combine(
                _sandboxRoot,
                "wwwroot");

        Assert.Throws<InvalidOperationException>(
            () =>
                ProtectedStorageRootValidator
                    .ValidateAndNormalize(
                        "relative-cv-storage",
                        webRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(
                _sandboxRoot))
        {
            Directory.Delete(
                _sandboxRoot,
                recursive:
                    true);
        }
    }
}
