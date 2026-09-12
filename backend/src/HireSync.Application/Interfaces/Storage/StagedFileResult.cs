namespace HireSync.Application.Interfaces.Storage;

public sealed record StagedFileResult(
    string StoredFileName,
    string StagingRelativePath,
    string FinalRelativePath,
    long SizeBytes,
    string Sha256Hash);
