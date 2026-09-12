namespace HireSync.Infrastructure.Storage;

public static class ProtectedStorageRootValidator
{
    public static string ValidateAndNormalize(
        string configuredRootPath,
        string publicWebRootPath)
    {
        if (string.IsNullOrWhiteSpace(
                configuredRootPath))
        {
            throw new InvalidOperationException(
                "CV storage root is not configured.");
        }

        if (!Path.IsPathFullyQualified(
                configuredRootPath))
        {
            throw new InvalidOperationException(
                "CV storage root must be an absolute path.");
        }

        if (string.IsNullOrWhiteSpace(
                publicWebRootPath))
        {
            throw new InvalidOperationException(
                "Public web root could not be resolved.");
        }

        if (!Path.IsPathFullyQualified(
                publicWebRootPath))
        {
            throw new InvalidOperationException(
                "Public web root must be an absolute path.");
        }

        string storageRoot;
        string webRoot;

        try
        {
            storageRoot =
                NormalizeAbsolutePath(
                    configuredRootPath);

            webRoot =
                NormalizeAbsolutePath(
                    publicWebRootPath);
        }
        catch (Exception exception)
            when (exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new InvalidOperationException(
                "CV storage root configuration is invalid.",
                exception);
        }

        var comparison =
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        if (string.Equals(
                storageRoot,
                webRoot,
                comparison) ||
            IsDescendantOf(
                storageRoot,
                webRoot,
                comparison))
        {
            throw new InvalidOperationException(
                "CV storage root must be outside the public web root.");
        }

        return storageRoot;
    }

    private static string NormalizeAbsolutePath(
        string path)
    {
        var fullPath =
            Path.GetFullPath(
                path);

        var root =
            Path.GetPathRoot(
                fullPath);

        if (!string.IsNullOrEmpty(root) &&
            string.Equals(
                fullPath,
                root,
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

    private static bool IsDescendantOf(
        string candidatePath,
        string parentPath,
        StringComparison comparison)
    {
        var prefix =
            parentPath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        return candidatePath.StartsWith(
            prefix,
            comparison);
    }
}
