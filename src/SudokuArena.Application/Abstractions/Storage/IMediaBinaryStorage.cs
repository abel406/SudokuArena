namespace SudokuArena.Application.Abstractions.Storage;

public interface IMediaBinaryStorage
{
    Task<StoredMediaFile> SaveAsync(
        Guid mediaId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken);
}

public sealed record StoredMediaFile(string StoragePath, long SizeBytes);
