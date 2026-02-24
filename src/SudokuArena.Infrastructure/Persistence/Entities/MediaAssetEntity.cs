namespace SudokuArena.Infrastructure.Persistence.Entities;

public sealed class MediaAssetEntity
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }
}
