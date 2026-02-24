namespace SudokuArena.Application.Contracts.Theming;

public sealed record MediaAssetSnapshot(
    Guid MediaId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedUtc,
    string PublicUrl);
