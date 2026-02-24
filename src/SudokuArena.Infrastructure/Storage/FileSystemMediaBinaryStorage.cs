using SudokuArena.Application.Abstractions.Storage;
using SudokuArena.Infrastructure.Configuration;

namespace SudokuArena.Infrastructure.Storage;

public sealed class FileSystemMediaBinaryStorage(MediaStorageOptions options) : IMediaBinaryStorage
{
    public async Task<StoredMediaFile> SaveAsync(
        Guid mediaId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        EnsureRootPathExists();
        var extension = Path.GetExtension(fileName);
        var sanitizedExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension.ToLowerInvariant();
        var relativePath = $"{mediaId:D}{sanitizedExtension}";
        var fullPath = Path.GetFullPath(Path.Combine(options.RootPath, relativePath));

        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, cancellationToken);
        await target.FlushAsync(cancellationToken);

        var fileInfo = new FileInfo(fullPath);
        return new StoredMediaFile(relativePath, fileInfo.Length);
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureRootPathExists();

        var fullPath = Path.GetFullPath(Path.Combine(options.RootPath, storagePath));
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        var stream = (Stream)File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    private void EnsureRootPathExists()
    {
        var fullRoot = Path.GetFullPath(options.RootPath);
        Directory.CreateDirectory(fullRoot);
    }
}
