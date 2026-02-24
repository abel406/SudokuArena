namespace SudokuArena.Domain.Models;

public sealed class MediaAsset
{
    public MediaAsset(
        Guid id,
        string fileName,
        string contentType,
        string storagePath,
        long sizeBytes,
        DateTimeOffset createdUtc)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("ContentType is required.", nameof(contentType));
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("StoragePath is required.", nameof(storagePath));
        }

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "SizeBytes cannot be negative.");
        }

        Id = id;
        FileName = fileName;
        ContentType = contentType;
        StoragePath = storagePath;
        SizeBytes = sizeBytes;
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public string StoragePath { get; }

    public long SizeBytes { get; }

    public DateTimeOffset CreatedUtc { get; }
}
