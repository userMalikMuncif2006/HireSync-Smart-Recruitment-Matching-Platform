namespace HireSync.Application.Interfaces.Storage;

public interface IFileStorage
{
    Task<StagedFileResult> StageAsync(
        Stream source,
        string extension,
        long maximumBytes,
        CancellationToken cancellationToken = default);

    Task PromoteAsync(
        StagedFileResult stagedFile,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(
        string relativePath,
        CancellationToken cancellationToken = default);
}
