namespace HireSync.Application.Interfaces.Storage;

public sealed class FileStorageSizeLimitExceededException
    : Exception
{
    public FileStorageSizeLimitExceededException(
        long maximumBytes)
        : base(
            $"The file exceeds the configured maximum of {maximumBytes} bytes.")
    {
        MaximumBytes = maximumBytes;
    }

    public long MaximumBytes { get; }
}
